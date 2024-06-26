
class Program {

  public static void Main() {
    Setup_ViewWorkspacesAndCapacities();
    // Demo01_CreateCustomerTenant();
    // Demo02_CreateImportedSalesModel();
    // Demo03_CreateTenantWithLakehouseAndNotebook();
    // Demo04_CreateTenantWithWarehouseAndDataPipelines();
    // Demo05_CreateTenantWithShortcutAndLoadTableApi();
    // Demo06_CreateTenantWithWarehouseAndSqlClient();
    // Demo07_CreateTenantWithAdlsGen2Api();
  }

  public static void Setup_ViewWorkspacesAndCapacities() {
    CustomerTenantBuilder.ViewWorkspaces();
    CustomerTenantBuilder.ViewCapacities();
  }

  public static void Demo01_CreateCustomerTenant() {
    string workspaceName = "Demo01 - Create Customer Tenant";
    //CustomerTenantBuilder.CreateCustomerTenant(workspaceName);
    CustomerTenantBuilder.CreateCustomerTenantWithUsers(workspaceName);
  }

  public static void Demo02_CreateImportedSalesModel() {

    string workspaceName = "Demo02 - Create Tenant with Imported Sales Model";

    CustomerTenantBuilder.CreateTenantWithImportedSalesModel(workspaceName);
    // CustomerTenantBuilder.ExportItemDefinitionsFromWorkspace(workspaceName);
    // CustomerTenantBuilder.UpdateSalesModel(workspaceName);
    // CustomerTenantBuilder.UpdateSalesReport(workspaceName);

  }

  public static void Demo03_CreateTenantWithLakehouseAndNotebook() {

    string workspaceName = "Demo03 - Create Tenant with Lakehouse and Notebook";
    CustomerTenantBuilder.CreateTenantWithLakehouseAndNotebook(workspaceName);

  }

  public static void Demo04_CreateTenantWithWarehouseAndDataPipelines() {

    string workspaceName = "Demo04 - Create Tenant with Warehouse and DataPipelines";
    CustomerTenantBuilder.CreateTenantWithWarehouseAndDataPipelines(workspaceName);

  }

  public static void Demo05_CreateTenantWithShortcutAndLoadTableApi() {

    string workspaceName = "Demo05 - Create Tenant with Shortcut and LoadTable API";
    CustomerTenantBuilder.CreateTenantWithShortcutAndLoadTableApi(workspaceName);
  }

  public static void Demo06_CreateTenantWithWarehouseAndSqlClient() {
    string workspaceName = "Demo06 - Create Tenant with Warehouse and SqlClient";
    CustomerTenantBuilder.CreateTenantWithWarehouseAndSqlClient(workspaceName);
  }

  public static void Demo07_CreateTenantWithAdlsGen2Api() {
    string workspaceName = "Demo07 - Create Tenant with ADLS Gen2 API";
    CustomerTenantBuilder.CreateTenantWithAdlsGen2Api(workspaceName);
  }

}