using Azure.Identity;
using Microsoft.Identity.Client;
using System.Reflection;

public enum AppAuthenticationMode {
  ServicePrincipalAuth,
  UserAuth,
  UserAuthWithAzurePowershell
}

public class EntraIdTokenManager {

  static EntraIdTokenManager() {
    switch (AppSettings.AuthenticationMode) {
      case AppAuthenticationMode.ServicePrincipalAuth:
        AppLogger.LogStep("Application configured to authenticate as service principal");
        break;
      case AppAuthenticationMode.UserAuth:
        AppLogger.LogStep("Application configured to authenticate as user");
        break;
      case AppAuthenticationMode.UserAuthWithAzurePowershell:
        AppLogger.LogStep("Application configured to authenticate as user with Azure PowerShell");
        break;
    }
  }

  // public methods
  public static AuthenticationResult GetAccessTokenResult(string[] scopes) {

    switch (AppSettings.AuthenticationMode) {

      case AppAuthenticationMode.ServicePrincipalAuth:
        return GetAccessTokenResultForServicePrincipal(scopes);

      case AppAuthenticationMode.UserAuth:
        return GetAccessTokenResultForUser(scopes);

      case AppAuthenticationMode.UserAuthWithAzurePowershell:
        return GetAccessTokenResultForAzurePowershell(scopes);

      default:
        return null;
    }
  }

  public static string GetFabricAccessToken() {

    switch (AppSettings.AuthenticationMode) {

      case AppAuthenticationMode.ServicePrincipalAuth:
        return GetAccessTokenResultForServicePrincipal(FabricPermissionScopes.Default).AccessToken;

      case AppAuthenticationMode.UserAuth:
        return GetAccessTokenResultForUser(FabricPermissionScopes.TenantProvisioning).AccessToken;

      case AppAuthenticationMode.UserAuthWithAzurePowershell:
        return GetAccessTokenResultForAzurePowershell(FabricPermissionScopes.User_Impersonation).AccessToken;

      default:
        return null;
    }
  }


  // private impelemntation details

  private const string tenantCommonAuthority = "https://login.microsoftonline.com/organizations";

  private static AuthenticationResult GetAccessTokenResultForServicePrincipal(string[] Scopes) {

    // Azure AD Application Id for service principal authentication
    string clientId = AppSettings.ServicePrincipalAuthClientId;
    string clientSecret = AppSettings.ServicePrincipalAuthClientSecret;
    string tenantId = AppSettings.ServicePrincipalAuthTenantId;
    string tenantSpecificAuthority = "https://login.microsoftonline.com/" + tenantId;

    var appConfidential =
        ConfidentialClientApplicationBuilder.Create(clientId)
          .WithClientSecret(clientSecret)
          .WithAuthority(tenantSpecificAuthority)
          .Build();

    return appConfidential.AcquireTokenForClient(Scopes).ExecuteAsync().Result;

  }

  private string GetAccessTokeForManagedIdentity() {

    string clientId = AppSettings.managedIdentityClientId;
    string[] scopes = new string[] { "https://api.fabric.microsoft.com/.default" };

    ManagedIdentityCredential credential = new Azure.Identity.ManagedIdentityCredential(clientId);
    Azure.Core.AccessToken token = credential.GetToken(new Azure.Core.TokenRequestContext(scopes));
    return token.Token;
  }

  private static AuthenticationResult GetAccessTokenResultForUser(string[] scopes) {

    string clientId = AppSettings.UserAuthClientId;
    string redirectUri = AppSettings.UserAuthRedirectUri;

    // create new public client application
    var appPublic = PublicClientApplicationBuilder.Create(clientId)
                    .WithAuthority(tenantCommonAuthority)
                    .WithRedirectUri(redirectUri)
                    .Build();

    // connect application to token cache
    TokenCacheHelper.EnableSerialization(appPublic.UserTokenCache);

    AuthenticationResult authResult;
    try {
      // try to acquire token from token cache
      var user = appPublic.GetAccountsAsync().Result.FirstOrDefault();
      authResult = appPublic.AcquireTokenSilent(scopes, user).ExecuteAsync().Result;
    }
    catch {
      authResult = appPublic.AcquireTokenInteractive(scopes).ExecuteAsync().Result;
    }

    // return access token to caller
    return authResult;
  }

  private static AuthenticationResult GetAccessTokenResultForAzurePowershell(string[] scopes) {

    // Azure PowerShell application uses the same client Id across all Entra Id tenants
    const string azurePowershellClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
    const string azurePowershellRedirectUri = "http://localhost";

    // create new public client application
    var appPublic = PublicClientApplicationBuilder.Create(azurePowershellClientId)
                    .WithRedirectUri(azurePowershellRedirectUri)
                    .WithAuthority(tenantCommonAuthority)
                    .Build();

    // connect application to token cache
    TokenCacheHelper.EnableSerialization(appPublic.UserTokenCache);

    AuthenticationResult authResult;
    try {
      // try to acquire token from token cache
      var user = appPublic.GetAccountsAsync().Result.FirstOrDefault();
      authResult = appPublic.AcquireTokenSilent(scopes, user).ExecuteAsync().Result;
    }
    catch {
      authResult = appPublic.AcquireTokenInteractive(scopes).ExecuteAsync().Result;
    }

    // return access token result to caller
    return authResult;

  }

  public static string GetAccessTokenForSqlEndPoint() {

    // Multitenant Application Id used by Microsoft.Data.SqlClient - available in any tenant
    string sqlAppId = "2fd908ad-0664-4344-b9be-cd3e8b574c38";

    string[] scopes = new string[] {
        "https://database.windows.net//.default"
      };

    // create new public client application
    var appPublic = PublicClientApplicationBuilder.Create(sqlAppId)
                    .WithAuthority(tenantCommonAuthority)
                    .WithRedirectUri("http://localhost")
                    .Build();

    // connect application to token cache
    TokenCacheHelper.EnableSerialization(appPublic.UserTokenCache);

    AuthenticationResult authResult;
    try {
      // try to acquire token from token cache
      var user = appPublic.GetAccountsAsync().Result.FirstOrDefault();
      authResult = appPublic.AcquireTokenSilent(scopes, user).ExecuteAsync().Result;
    }
    catch {
      authResult = appPublic.AcquireTokenInteractive(scopes).ExecuteAsync().Result;
    }

    // return access token to caller
    return authResult.AccessToken;
  }

  // utility class used to assist with token caching for user tokens 
  static class TokenCacheHelper {

    private static readonly string CacheFilePath = Assembly.GetExecutingAssembly().Location + ".tokencache.json";
    private static readonly object FileLock = new object();

    public static void EnableSerialization(ITokenCache tokenCache) {
      tokenCache.SetBeforeAccess(BeforeAccessNotification);
      tokenCache.SetAfterAccess(AfterAccessNotification);
    }

    private static void BeforeAccessNotification(TokenCacheNotificationArgs args) {
      lock (FileLock) {
        // repopulate token cache from persisted store
        args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath) ? File.ReadAllBytes(CacheFilePath) : null);
      }
    }

    private static void AfterAccessNotification(TokenCacheNotificationArgs args) {
      // if the access operation resulted in a cache update
      if (args.HasStateChanged) {
        lock (FileLock) {
          // write token cache changes to persistent store
          File.WriteAllBytes(CacheFilePath, args.TokenCache.SerializeMsalV3());
        }
      }
    }

  }



}

