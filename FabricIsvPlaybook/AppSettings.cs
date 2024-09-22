

public class AppSettings {

  public const string FabricUserApiBaseUrl = "https://api.fabric.microsoft.com/v1";
  public const string PowerBiRestApiBaseUrl = "https://api.powerbi.com";
  public const string OneLakeBaseUrl = "https://onelake.dfs.fabric.microsoft.com";

  // TODO: add Capacity Id for Fabric-enabled Premium capacity
  public const string PremiumCapacityId = "11111111-1111-1111-1111-111111111111";

  public static AppAuthenticationMode AuthenticationMode = AppAuthenticationMode.UserAuthWithAzurePowershell;

  // Public client application created in Entra Id Service for user auth
  public const string UserAuthClientId = "22222222-2222-2222-2222-222222222222";
  public const string UserAuthRedirectUri = "http://localhost";

  // Condifential client application created in Entra Id Service for service principal auth
  public const string ServicePrincipalAuthTenantId = "33333333-3333-3333-3333-333333333333";
  public const string ServicePrincipalAuthClientId = "44444444-4444-4444-4444-444444444444";
  public const string servicePrincipalAuthClientSecret = "ADD_CLIENT_SECRET_HERE";

  // Managed identity created in Azure for service principal auth
  public const string managedIdentityClientId = "55555555-5555-5555-55555-555555555555";


  // Add Azure AD object Ids for 2 users, a group and a service principal for testing role assignments  
  public const string AdminUser1Id = "66666666-6666-6666-6666-666666666666";
  public const string TestUser1Id = "77777777-7777-7777-7777-777777777777";
  public const string TestUser2Id = "88888888-8888-8888-8888-888888888888";
  public const string TestADGroup1 = "99999999-9999-9999-9999-999999999999";
  public const string ServicePrincipalObjectId = "00000000-0000-0000-0000-000000000000";

  public const string LocalTemplatesFolder = @"..\..\..\ItemDefinitionExports\";
  public const string LocalDataFilesFolder = @"..\..\..\DataFiles\";

  // TODO: add configuration info and connection ID for Azue Storage
  public const string AzureStorageServer = "https://YOUR_AZURE_STORAGE_ACCOUNT.dfs.core.windows.net";
  public const string AzureStoragePath = "/";
  public const string AzureStorageAccountKey = "YOUR_AZURE_STORAGE_ACCOUNT_KEY";

  public const string SqlServer = "YOUR_AZURE_SQL_SERVER.database.windows.net";
  public const string SqlDatabase = "YOUR_AZURE_SQL_DATABASE";
  public const string SqlUser = "YOUR_AZURE_SQL_USERNAME";
  public const string SqlUserPassword = "YOUR_AZURE_SQL_PASSWORD";

  public const string WebUrlForData = "https://github.com/PowerBiDevCamp/ProductSalesData/raw/main/data";

}
