
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;

class Program {

  public static void Main() {

    AppSettings.AuthenticationMode = AppAuthenticationMode.UserAuth;

    Setup_ViewWorkspacesAndCapacities();
    // Demo01_CreateWorkspace();
    // Demo02_CreateConnections();
    // Demo03_DeploySolutionWithImportedSalesModel();
    // Demo04_DeploySolutionWithLakehouseAndNotebook();
    // Demo05_DeploySolutionWithWarehouseAndDataPipelines();
    // Demo06_DeploySolutionWithShortcutAndLoadTableApi();
    // Demo07_DeploySolutionWithWarehouseAndSqlClient();
    // Demo08_DeploySolutionWithAdlsGen2Api();
    // Demo09_BranchOutToFeatureWorkspace();
  }

  public static void Setup_ViewWorkspacesAndCapacities() {
    CustomerTenantBuilder.ViewWorkspaces();
    CustomerTenantBuilder.ViewCapacities();
  }

  public static void Demo01_CreateWorkspace() {
    string workspaceName = "Demo01 - Create Workspace for Customer Tenant";
    //CustomerTenantBuilder.CreateWorkspaceForCustomerTenant(workspaceName);
     CustomerTenantBuilder.CreateWorkspaceWithRoleAssignments(workspaceName);
  }

  public static void Demo02_CreateConnections() {
    string workspaceName = "Demo02 - Create Connections";
    CustomerTenantBuilder.CreateConnections(workspaceName);
  }

  public static void Demo03_DeploySolutionWithImportedSalesModel() {

    string workspaceName = "Demo03 - Deploy solution with Imported Sales Model";
    
    CustomerTenantBuilder.DeploySolutionWithImportedSalesModel(workspaceName);
    // CustomerTenantBuilder.ExportItemDefinitionsFromWorkspace(workspaceName);
    // CustomerTenantBuilder.UpdateProductSalesSemanticModel(workspaceName);
    // CustomerTenantBuilder.UpdateProductSalesReport(workspaceName);
    // CustomerTenantBuilder.CloneWorkspace(workspaceName, "Clone of " + workspaceName);
  }

  public static void Demo04_DeploySolutionWithLakehouseAndNotebook() {
    string workspaceName = "Demo04 - Deploy solution with Lakehouse and Notebook";
    CustomerTenantBuilder.DeploySolutionWithLakehouseAndNotebook(workspaceName);
  }

  public static void Demo05_DeploySolutionWithWarehouseAndDataPipelines() {
    string workspaceName = "Demo05 - Deploy solution with Warehouse and DataPipelines";
    CustomerTenantBuilder.DeploySolutionWithWarehouseAndDataPipelines(workspaceName);
  }

  public static void Demo06_DeploySolutionWithShortcutAndLoadTableApi() {
    string workspaceName = "Demo06 - Deploy solution with OneLake Shortcut and LoadTable API";
    CustomerTenantBuilder.DeploySolutionWithWithShortcutAndLoadTableApi(workspaceName);
  } 

  public static void Demo07_DeploySolutionWithWarehouseAndSqlClient() {
    string workspaceName = "Demo07 - Deploy solution with Warehouse and SqlClient";
    CustomerTenantBuilder.DeploySolutionWithWarehouseAndSqlClient(workspaceName);
  }

  public static void Demo08_DeploySolutionWithAdlsGen2Api() {
    string workspaceName = "Demo08 - Deploy solution with ADLS Gen2 API";
    CustomerTenantBuilder.DeploySolutionWithAdlsGen2Api(workspaceName);
  }

  public static void Demo09_BranchOutToFeatureWorkspace() {
   
    string workspaceName = "Acme Corp";
    string featureName = "Feature1";
 
    // CustomerTenantBuilder.DeploySolutionWithLakehouseAndNotebook(workspaceName);
    // CustomerTenantBuilder.ExportItemDefinitionsFromWorkspace(workspaceName);
    CustomerTenantBuilder.BranchOutToFeatureWorkspace(workspaceName, featureName);

  }

  public static void HelloDotNetSdk() {

    string accessToken = EntraIdTokenManager.GetFabricAccessToken();

    FabricClient fabricApiClient = new FabricClient(accessToken);

    List<Workspace> workspaces = fabricApiClient.Core.Workspaces.ListWorkspaces().ToList();

    Console.WriteLine(" > Workspaces List");

    foreach (var workspace in workspaces) {
      Console.WriteLine("   - {0} ({1})", workspace.DisplayName, workspace.Id);
    }

  }


}