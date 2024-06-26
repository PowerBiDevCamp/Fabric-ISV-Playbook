using Microsoft.Identity.Client;
using System.Reflection;

public class EntraIdTokenManager {

  private const string tenantCommonAuthority = "https://login.microsoftonline.com/organizations";

  // Azure AD Application Id for user authentication
  private static string applicationId = AppSettings.ApplicationId;
  private static string redirectUri = AppSettings.RedirectUri;

  public static string GetAccessToken(string[] scopes) {

    // create new public client application
    var appPublic = PublicClientApplicationBuilder.Create(applicationId)
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
    return authResult.AccessToken;
  }

  public static string GetAccessTokenInteractive(string[] scopes) {

    // create new public client application
    var appPublic = PublicClientApplicationBuilder.Create(applicationId)
                  .WithAuthority(tenantCommonAuthority)
                  .WithRedirectUri(redirectUri)
                  .Build();

    AuthenticationResult authResult = appPublic.AcquireTokenInteractive(scopes).ExecuteAsync().Result;

    // return access token to caller
    return authResult.AccessToken;
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
    return authResult.AccessToken;
  }

  public static AuthenticationResult GetAccessTokenResult(string[] scopes) {

    // create new public client application
    var appPublic = PublicClientApplicationBuilder.Create(applicationId)
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