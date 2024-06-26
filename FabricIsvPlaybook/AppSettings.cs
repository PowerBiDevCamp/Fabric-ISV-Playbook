
public class AppSettings {

  public const string FabricUserApiBaseUrl = "https://api.fabric.microsoft.com/v1";
  public const string PowerBiRestApiBaseUrl = "https://api.powerbi.com";
  public const string OneLakeBaseUrl = "https://onelake.dfs.fabric.microsoft.com";

  // use App Id for PowerShell Azure Auth - it works in every Fabric tenant
  public const string ApplicationId = "1950a258-227b-4e31-a9cf-717495945fc2";
  public const string RedirectUri = "http://localhost";

  // TODO: add Capacity Id for Fabric-enabled Premium capacity
  public const string PremiumCapacityId = "ADD_ID_FOR_CAPACITY_HERE";
  
  // TODO: add configuration info and connection ID for Azue Storage
  public const string AzureStorageServer = "https://YOUR_AZURE_STORAGE_ACCOUNT.dfs.core.windows.net";
  public const string AzureStoragePath = "/";
  public const string AzureStorageConnectId = "YOUR_AZURE_STORAGE_CONNECTION_ID";

  // Add Azure AD object Ids for 2 users, a group and a service principal for testing role assignments  // Add Azure AD object Ids for 2 users, a group and a service principal for testing role assignments
  public const string AdminUser1Id = "ADD_ID_FOR_ADMIN_USER_HERE";
  public const string TestUser1Id = "ADD_ID_FOR_TEST_USER1_HERE";
  public const string TestUser2Id = "ADD_ID_FOR_TES_USER2_HERE";
  public const string TestADGroup1 = "ADD_ID_FOR_AZURE_AD_GROUP_HERE";
  public const string ServicePrincipalObjectId = "ADD_OBJECT_ID_FOR_SERVICE_PRINCIPAL_HERE";

  public const string LocalWebPageFolder = @"..\..\..\WebPages\";
  public const string LocalTemplatesFolder = @"..\..\..\ItemDefinitionExports\";
  public const string LocalDataFilesFolder = @"..\..\..\DataFiles\";

 
}
