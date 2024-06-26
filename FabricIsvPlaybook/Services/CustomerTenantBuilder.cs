
using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;
using System.Diagnostics;
using static FabricUserApi;

class CustomerTenantBuilder {

  private const string ImportedModelName = "Product Sales Imported";
  private const string DirectLakeModelName = "Product Sales DirectLake";

  public static void ViewWorkspaces() {

    Console.WriteLine("View workspaces accessible to current user");
    Console.WriteLine();

    var workspcaes = FabricUserApi.GetWorkspaces();

    Console.WriteLine(" > Workspaces List");
    foreach (var workspace in workspcaes) {
      Console.WriteLine("   - {0} ({1})", workspace.DisplayName, workspace.Id);
    }

    Console.WriteLine();

  }

  public static void ViewCapacities() {

    Console.WriteLine("View capacities accessible to current user");
    Console.WriteLine();

    var capacities = FabricUserApi.GetCapacities();

    Console.WriteLine(" > Capacities List");
    foreach (var capacity in capacities) {
      Console.WriteLine("   - [{0}] {1} ({2})", capacity.Sku, capacity.DisplayName, capacity.Id);
    }

    Console.WriteLine();

  }

  public static void CreateCustomerTenant(string WorkspaceName) {

    Console.WriteLine("Create new Fabric workspace to serve as customer tenant");
    var workspace = FabricUserApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    Console.WriteLine();
    Console.WriteLine("Mission complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();

    OpenWorkspaceInBrowser(workspace.Id.ToString());

  }

  public static void CreateCustomerTenantWithUsers(string WorkspaceName) {

    AppLogger.LogSolution("Create new Fabric workspace and assign workspace roles to members");

    var workspace = FabricUserApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId, "Demo workspace");

    Guid AdminUserId = new Guid(AppSettings.AdminUserId);
    Guid TestUser1Id = new Guid(AppSettings.TestUser1Id);
    Guid TestUser2Id = new Guid(AppSettings.TestUser2Id);
    Guid TestADGroup1 = new Guid(AppSettings.TestADGroup1);

    FabricUserApi.AddUserAsWorkspaceMemeber(workspace.Id, TestUser1Id, WorkspaceRole.Admin);
    FabricUserApi.AddUserAsWorkspaceMemeber(workspace.Id, TestUser2Id, WorkspaceRole.Viewer);
    FabricUserApi.AddGroupAsWorkspaceMemeber(workspace.Id, TestADGroup1, WorkspaceRole.Member);

    FabricUserApi.ViewWorkspaceRoleAssignments(workspace.Id);

    Console.WriteLine();
    Console.WriteLine("Mission complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();

    OpenWorkspaceInBrowser(workspace.Id.ToString());

  }

  public static void ExportItemDefinitionsFromWorkspace(string WorkspaceName) {
    FabricUserApi.ExportItemDefinitionsFromWorkspace(WorkspaceName);
  }

  public static void CreateTenantWithImportedSalesModel(string WorkspaceName) {

    AppLogger.LogSolution("Create Tenant with Imported Sales Model");

    var workspace = FabricUserApi.CreateWorkspace(WorkspaceName);

    var modelCreateRequest =
      FabricItemDefinitionFactory.GetImportedSalesModelCreateRequest(ImportedModelName);

    var model = FabricUserApi.CreateItem(workspace.Id, modelCreateRequest);


    PowerBiUserApi.PatchAnonymousAccessWebCredentials(workspace.Id, model.Id.Value);

    PowerBiUserApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, ImportedModelName);

    var report = FabricUserApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    // uncomment next two lines to test Power BI embedding
    // WebPageGenerator.GenerateReportPageUserOwnsData(workspace.id, report.id);
    // WebPageGenerator.GenerateReportPageAppOwnsData(workspace.id, report.id);

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void UpdateSalesModel(string WorkspaceName) {

    Workspace workspace = FabricUserApi.GetWorkspaceByName(WorkspaceName);

    var model = FabricUserApi.GetSemanticModelByName(workspace.Id, ImportedModelName);

    AppLogger.LogOperationStart($"Updating item definition for semantic model named {model.DisplayName}");

    var updateModelRequest =
      FabricItemDefinitionFactory.GetImportedSalesModelUpdateRequest(ImportedModelName);

    FabricUserApi.UpdateItemDefinition(workspace.Id, model.Id.Value, updateModelRequest);

    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Customer tenant update complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void UpdateSalesReport(string WorkspaceName) {

    Workspace workspace = FabricUserApi.GetWorkspaceByName(WorkspaceName);

    var model = FabricUserApi.GetSemanticModelByName(workspace.Id, ImportedModelName);
    var report = FabricUserApi.GetReportByName(workspace.Id, ImportedModelName);

    AppLogger.LogOperationStart($"Updating item definition for report named {report.DisplayName}");

    var updateReportRequest =
      FabricItemDefinitionFactory.GetSalesReportUpdateRequest(model.Id.Value, ImportedModelName);

    FabricUserApi.UpdateItemDefinition(workspace.Id, report.Id.Value, updateReportRequest);

    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Customer tenant update complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void CreateTenantWithLakehouseAndNotebook(string WorkspaceName) {

    string lakehouseName = "sales";
    string notebookName = "Create Lakehouse Tables";

    AppLogger.LogSolution("Create Tenant with Lakehouse, Notebook and DirectLake Semantic Model");

    var workspace = FabricUserApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    var lakehouse = FabricUserApi.CreateLakehouse(workspace.Id, lakehouseName);

    string codeContent = FabricIsvPlaybook.Properties.Resources.CreateLakehouseTables_ipynb;
    var notebookCreateRequest = FabricItemDefinitionFactory.GetNotebookCreateRequest(workspace.Id, lakehouse, notebookName, codeContent);

    var notebook = FabricUserApi.CreateItem(workspace.Id, notebookCreateRequest);

    FabricUserApi.RunNotebook(workspace.Id, notebook);

    AppLogger.LogOperationStart("Getting SQL endpoint connection information");
    var sqlEndpoint = FabricUserApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    var modelCreateRequest =
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest(DirectLakeModelName, sqlEndpoint.ConnectionString, sqlEndpoint.Id);

    var model = FabricUserApi.CreateItem(workspace.Id, modelCreateRequest);

    PowerBiUserApi.PatchDirectLakeDatasetCredentials(workspace.Id, model.Id.Value);

    PowerBiUserApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, DirectLakeModelName);

    var report = FabricUserApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void CreateTenantWithWarehouseAndDataPipelines(string WorkspaceName) {

    Console.WriteLine("Create Tenant with Warehouse and DataPipelines");

    string warehouseName = "sales";
    string LakehouseName = "staging";

    var workspace = FabricUserApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    var lakehouseItem = FabricUserApi.CreateLakehouse(workspace.Id, LakehouseName);

    var lakehouse = FabricUserApi.GetLakehouse(workspace.Id, lakehouseItem.Id.Value);

    string server = AppSettings.AzureStorageServer;
    string path = AppSettings.AzureStoragePath;
    string connectionName = $"{WorkspaceName}-{server}:{path}";

    var azureSotrageConnectionId = AppSettings.AzureStorageConnectId;

    // create and run data pipeline 1
    string dataPipelineName1 = "Copy Data Files To Staging Lakehouse";
    var createDataPipelineRequest1 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForLakehouse(
                                    dataPipelineName1,
                                    FabricIsvPlaybook.Properties.Resources.CopySalesData_json,
                                    workspace.Id.ToString(),
                                    lakehouse.Id.Value.ToString(),
                                    azureSotrageConnectionId);

    var pipeline1 = FabricUserApi.CreateItem(workspace.Id, createDataPipelineRequest1);

    FabricUserApi.RunDataPipeline(workspace.Id, pipeline1);

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for lakehouse");
    var sqlEndpointLakehouse = FabricUserApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {sqlEndpointLakehouse.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpointLakehouse.Id);

    var warehouse = FabricUserApi.CreateWarehouse(workspace.Id, warehouseName);

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for data warehouse");
    string warehouseConnectString = FabricUserApi.GetWarehouseConnection(workspace.Id, warehouse.Id.Value);
    AppLogger.LogOperationComplete();


    // create and run data pipeline 2
    string dataPipeLineName2 = "Create Warehouse Tables";
    var createDataPipelineRequest2 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName2,
                                    FabricIsvPlaybook.Properties.Resources.CreateWarehouseTables_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline2 = FabricUserApi.CreateItem(workspace.Id, createDataPipelineRequest2);

    FabricUserApi.RunDataPipeline(workspace.Id, pipeline2);

    // create and run data pipeline 3
    string dataPipeLineName3 = "Create Warehouse Stored Procedures";
    var createDataPipelineRequest3 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName3,
                                    FabricIsvPlaybook.Properties.Resources.CreateStoredProcedures_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline3 = FabricUserApi.CreateItem(workspace.Id, createDataPipelineRequest3);

    FabricUserApi.RunDataPipeline(workspace.Id, pipeline3);


    // create and run data pipeline 4
    string dataPipeLineName4 = "Refresh All Warehouse Tables";
    var createDataPipelineRequest4 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName4,
                                    FabricIsvPlaybook.Properties.Resources.RefreshAllTables_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline4 = FabricUserApi.CreateItem(workspace.Id, createDataPipelineRequest4);

    FabricUserApi.RunDataPipeline(workspace.Id, pipeline4);


    var modelCreateRequest =
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest("Product Sales", warehouseConnectString, warehouseName);

    var model = FabricUserApi.CreateItem(workspace.Id, modelCreateRequest);

    PowerBiUserApi.PatchDirectLakeDatasetCredentials(workspace.Id, model.Id.Value);

    PowerBiUserApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, "Product Sales");

    var report = FabricUserApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void CreateTenantWithShortcutAndLoadTableApi(string WorkspaceName) {

    string warehouseName = "sales";
    string lakehouseName = "staging";

    AppLogger.LogSolution("Create Tenant with Shortcut and Fabric LoadTable API");

    Workspace workspace = FabricUserApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    Item lakehouse = FabricUserApi.CreateLakehouse(workspace.Id, lakehouseName);

    Uri shortcutUri = new Uri(AppSettings.AzureStorageServer);
    string shortcutSubpath = "/sales-data";
    Guid connectionId = new Guid(AppSettings.AzureStorageConnectId);

    Target shortcutTarget = new Target();
    shortcutTarget.AdlsGen2 = new AdlsGen2(shortcutUri, shortcutSubpath, connectionId);

    var createShortcutRequest = new CreateShortcutRequest("Files", "sales-data", shortcutTarget);

    AppLogger.LogStep($"Create Shortcut to ADLS Gen2 Storage with path {AppSettings.AzureStorageServer}");
    FabricUserApi.CreateLakehouseShortcut(workspace.Id, lakehouse.Id.Value, createShortcutRequest);

    AppLogger.LogStep("Loading delta tables in lakehouse from data files using Fabric Load Table API");

    AppLogger.LogSubstep("Loading table from Files/sales-data/products.csv");
    FabricUserApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/products.csv", "products");
 
    AppLogger.LogSubstep("Loading table from Files/sales-data/customers.csv");
    FabricUserApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/customers.csv", "customers");

    AppLogger.LogSubstep("Loading table from Files/sales-data/invoices.csv");
    FabricUserApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/invoices.csv", "invoices");

    AppLogger.LogSubstep("Loading table from Files/sales-data/invoice_details.csv");
    FabricUserApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/invoice_details.csv", "invoice_details");

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for lakehouse");
    var sqlEndpointLakehouse = FabricUserApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {sqlEndpointLakehouse.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpointLakehouse.Id);

    var warehouse = FabricUserApi.CreateWarehouse(workspace.Id, warehouseName);

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for data warehouse");
    string warehouseConnectString = FabricUserApi.GetWarehouseConnection(workspace.Id, warehouse.Id.Value);
    AppLogger.LogOperationComplete();


    // create and run data pipeline 2
    string dataPipeLineName2 = "Create Warehouse Tables";
    var createDataPipelineRequest2 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName2,
                                    FabricIsvPlaybook.Properties.Resources.CreateWarehouseTables_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline2 = FabricUserApi.CreateItem(workspace.Id, createDataPipelineRequest2);

    FabricUserApi.RunDataPipeline(workspace.Id, pipeline2);

    // create and run data pipeline 3
    string dataPipeLineName3 = "Create Warehouse Stored Procedures";
    var createDataPipelineRequest3 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName3,
                                    FabricIsvPlaybook.Properties.Resources.CreateStoredProcedures_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline3 = FabricUserApi.CreateItem(workspace.Id, createDataPipelineRequest3);

    FabricUserApi.RunDataPipeline(workspace.Id, pipeline3);


    // create and run data pipeline 4
    string dataPipeLineName4 = "Refresh All Warehouse Tables";
    var createDataPipelineRequest4 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName4,
                                    FabricIsvPlaybook.Properties.Resources.RefreshAllTables_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline4 = FabricUserApi.CreateItem(workspace.Id, createDataPipelineRequest4);

    FabricUserApi.RunDataPipeline(workspace.Id, pipeline4);


    var modelCreateRequest =
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest("Product Sales", warehouseConnectString, warehouseName);

    var model = FabricUserApi.CreateItem(workspace.Id, modelCreateRequest);

    PowerBiUserApi.PatchDirectLakeDatasetCredentials(workspace.Id, model.Id.Value);

    PowerBiUserApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, "Product Sales");

    var report = FabricUserApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void CreateTenantWithWarehouseAndSqlClient(string WorkspaceName) {

    string warehouseName = "sales";
    string lakehouseName = "staging";

    AppLogger.LogSolution("Create Tenant with Warehouse and Microsoft.Data.SqlClient");

    Workspace workspace = FabricUserApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    Item lakehouse = FabricUserApi.CreateLakehouse(workspace.Id, lakehouseName);

    Uri shortcutUri = new Uri(AppSettings.AzureStorageServer);
    string shortcutSubpath = "/sales-data";
    Guid connectionId = new Guid(AppSettings.AzureStorageConnectId);

    Target shortcutTarget = new Target();
    shortcutTarget.AdlsGen2 = new AdlsGen2(shortcutUri, shortcutSubpath, connectionId);

    var createShortcutRequest = new CreateShortcutRequest("Files", "sales-data", shortcutTarget);

    AppLogger.LogStep($"Create Shortcut to ADLS Gen2 Storage with path {AppSettings.AzureStorageServer}");
    FabricUserApi.CreateLakehouseShortcut(workspace.Id, lakehouse.Id.Value, createShortcutRequest);

    AppLogger.LogStep("Loading delta tables in lakehouse from data files using Fabric Load Table API");

    AppLogger.LogSubstep("Loading table from Files/sales-data/products.csv");
    FabricUserApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/products.csv", "products");

    AppLogger.LogSubstep("Loading table from Files/sales-data/customers.csv");
    FabricUserApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/customers.csv", "customers");

    AppLogger.LogSubstep("Loading table from Files/sales-data/invoices.csv");
    FabricUserApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/invoices.csv", "invoices");

    AppLogger.LogSubstep("Loading table from Files/sales-data/invoice_details.csv");
    FabricUserApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/invoice_details.csv", "invoice_details");

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for lakehouse");
    var sqlEndpointLakehouse = FabricUserApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {sqlEndpointLakehouse.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpointLakehouse.Id);


    var warehouseItem = FabricUserApi.CreateWarehouse(workspace.Id, warehouseName);

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for data warehouse");
    string connectionInfo = FabricUserApi.GetWarehouseConnection(workspace.Id, warehouseItem.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {connectionInfo}");
    AppLogger.LogSubstep($"Database: {warehouseItem.Id.ToString()}");

    AppLogger.LogStep("Connecting to warehouse SQL Endpoint to execute SQL commands");
    var sqlWriter = new SqlConnectionWriter(connectionInfo, warehouseName);

    AppLogger.LogSubstep("Executing SQL commands to create stored procedures and warehouse tables");
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_CreateAllTables_sql);
    sqlWriter.ExecuteSql("EXEC create_all_tables");
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_RefreshProducts_sql);
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_RefreshCustomers_sql);
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_RefreshSales_sql);
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_RefreshCalendar_sql);

    AppLogger.LogSubstep("Executing SQL stored proceedure to refresh products table");
    sqlWriter.ExecuteSql("EXEC refresh_products");

    AppLogger.LogSubstep("Executing SQL stored proceedure to refresh customers table");
    sqlWriter.ExecuteSql("EXEC refresh_customers");

    AppLogger.LogSubstep("Executing SQL stored proceedure to refresh sales table");
    sqlWriter.ExecuteSql("EXEC refresh_sales");

    AppLogger.LogSubstep("Executing SQL stored proceedure to refresh calendar table");
    sqlWriter.ExecuteSql("EXEC refresh_calendar");

    var modelCreateRequest =
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest("Product Sales", connectionInfo, warehouseName);

    var model = FabricUserApi.CreateItem(workspace.Id, modelCreateRequest);

    PowerBiUserApi.PatchDirectLakeDatasetCredentials(workspace.Id, model.Id.Value);

    PowerBiUserApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, "Product Sales");

    var report = FabricUserApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void CreateTenantWithAdlsGen2Api(string WorkspaceName) {

    string LakehouseName = "staging";
    string warehouseName = "sales";

    AppLogger.LogSolution("Provision tenant using ADLS Gen2 API and LoadTable API");

    var workspace = FabricUserApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    var lakehouseItem = FabricUserApi.CreateLakehouse(workspace.Id, LakehouseName);

    var lakehouse = FabricUserApi.GetLakehouse(workspace.Id, lakehouseItem.Id.Value);

    AppLogger.LogStep($"Connecting to OneLake storage of lakehouse using ADLS Gen2 API");
    var oneLakeWriter = new OneLakeFileWriter(workspace.Id, lakehouse.Id.Value);

    AppLogger.LogSubstep("Creating landing_zone folder in Files folder");
    var folder = oneLakeWriter.CreateTopLevelFolder("landing_zone");

    string dataFilesFolder = AppSettings.LocalDataFilesFolder;
    var parquet_files = Directory.GetFiles(dataFilesFolder, "*.parquet", SearchOption.AllDirectories);

    foreach (var parquet_file in parquet_files) {

      // STEP 1 - determine file name and table name
      var fileName = Path.GetFileName(parquet_file);
      var tableName = fileName.Replace(".parquet", "");

      // STEP 2 - Upload data file
      AppLogger.LogSubstep($"Uploading data file named {fileName} to OneLake storage in lakehouse");
      Stream content = new MemoryStream(File.ReadAllBytes(parquet_file));
      oneLakeWriter.CreateFile(folder, fileName, content);

      // STEP 3 - load data file to create delta table
      AppLogger.LogSubstep($"Calling LoadTable API on {fileName} to create {tableName} table");
      FabricUserApi.LoadLakehouseTableFromParquet(workspace.Id, lakehouse.Id.Value, "Files/landing_zone/" + fileName, tableName);
    }

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for lakehouse");
    var sqlEndpointLakehouse = FabricUserApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {sqlEndpointLakehouse.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpointLakehouse.Id);

    var warehouseItem = FabricUserApi.CreateWarehouse(workspace.Id, warehouseName);

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for data warehouse");
    string connectionInfo = FabricUserApi.GetWarehouseConnection(workspace.Id, warehouseItem.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {connectionInfo}");
    AppLogger.LogSubstep($"Database: {warehouseItem.Id.ToString()}");

    AppLogger.LogStep("Connecting to warehouse SQL Endpoint to execute SQL commands");
    var sqlWriter = new SqlConnectionWriter(connectionInfo, warehouseName);

    AppLogger.LogSubstep("Executing SQL commands to create stored procedures and lakehouse tables");
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_CreateAllTables_sql);
    sqlWriter.ExecuteSql("EXEC create_all_tables");
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_RefreshProducts_sql);
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_RefreshCustomers_sql);
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_RefreshSales_sql);
    sqlWriter.ExecuteSql(FabricIsvPlaybook.Properties.Resources.CreateSproc_RefreshCalendar_sql);

    AppLogger.LogSubstep("Executing SQL stored proceedure to refresh products table");
    sqlWriter.ExecuteSql("EXEC refresh_products");

    AppLogger.LogSubstep("Executing SQL stored proceedure to refresh customers table");
    sqlWriter.ExecuteSql("EXEC refresh_customers");

    AppLogger.LogSubstep("Executing SQL stored proceedure to refresh sales table");
    sqlWriter.ExecuteSql("EXEC refresh_sales");

    AppLogger.LogSubstep("Executing SQL stored proceedure to refresh calendar table");
    sqlWriter.ExecuteSql("EXEC refresh_calendar");

    var modelCreateRequest =
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest("Product Sales", connectionInfo, warehouseName);

    var model = FabricUserApi.CreateItem(workspace.Id, modelCreateRequest);

    PowerBiUserApi.PatchDirectLakeDatasetCredentials(workspace.Id, model.Id.Value);

    PowerBiUserApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, "Product Sales");

    var report = FabricUserApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  private static void OpenWorkspaceInBrowser(Guid WorkspaceId) {
    OpenWorkspaceInBrowser(WorkspaceId.ToString());
  }

  private static void OpenWorkspaceInBrowser(string WorkspaceId) {

    string url = "https://app.powerbi.com/groups/" + WorkspaceId;

    var process = new Process();
    process.StartInfo = new ProcessStartInfo(@"C:\Program Files\Google\Chrome\Application\chrome.exe");
    process.StartInfo.Arguments = url + " --profile-directory=\"Profile 2\" ";
    process.Start();

  }


}
