using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;
using Microsoft.Fabric.Api.Notebook.Models;
using System.Diagnostics;
using System.Text;

class CustomerTenantBuilder {

  private const string ImportedModelName = "Product Sales Imported";
  private const string DirectLakeModelName = "Product Sales DirectLake";

  public static void ViewWorkspaces() {

    var workspaces = FabricRestApi.GetWorkspaces();

    AppLogger.LogStep("Workspaces List");
    foreach (var workspace in workspaces) {
      AppLogger.LogSubstep($"{workspace.DisplayName} ({workspace.Id})");
    }

    Console.WriteLine();

  }

  public static void ViewCapacities() {

    var capacities = FabricRestApi.GetCapacities();

    AppLogger.LogStep("Capacities List");
    foreach (var capacity in capacities) {
      AppLogger.LogSubstep($"[{capacity.Sku}] {capacity.DisplayName} (ID={capacity.Id})");
    }

  }

  public static void CreateWorkspaceForCustomerTenant(string WorkspaceName) {

    Console.WriteLine("Create new Fabric workspace to serve as customer tenant");
    var workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    Console.WriteLine();
    Console.WriteLine("Mission complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();

    OpenWorkspaceInBrowser(workspace.Id.ToString());

  }

  public static void CreateWorkspaceWithRoleAssignments(string WorkspaceName) {

    AppLogger.LogSolution("Create new Fabric workspace and assign workspace roles to members");

    var workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId, "Demo workspace");

    Guid TestUser1Id = new Guid(AppSettings.TestUser1Id);
    Guid TestUser2Id = new Guid(AppSettings.TestUser2Id);
    Guid TestADGroup1 = new Guid(AppSettings.TestADGroup1);

    FabricRestApi.AddUserAsWorkspaceMemeber(workspace.Id, TestUser1Id, WorkspaceRole.Admin);
    FabricRestApi.AddUserAsWorkspaceMemeber(workspace.Id, TestUser2Id, WorkspaceRole.Viewer);
    FabricRestApi.AddGroupAsWorkspaceMemeber(workspace.Id, TestADGroup1, WorkspaceRole.Member);

    FabricRestApi.ViewWorkspaceRoleAssignments(workspace.Id);

    Console.WriteLine();
    Console.WriteLine("Mission complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();

    OpenWorkspaceInBrowser(workspace.Id.ToString());

  }

  public static void CreateConnections(string WorkspaceName) {

    string lakehouseName = "sales";

    AppLogger.LogSolution("Create Shortcut with Connection to ADLS Gen2");

    Workspace workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    Item lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);

    string path = "Files";
    string subpath = "sales-data";
    Uri location = new Uri(AppSettings.AzureStorageServer);

    var connection1 = FabricConnectionsApi.CreateAzureStorageConnection(AppSettings.AzureStorageServer,
                                                                       AppSettings.AzureStoragePath);

    FabricRestApi.CreateAdlsGen2Shortcut(workspace.Id,
                                         lakehouse.Id.Value,
                                         location,
                                         path,
                                         subpath,
                                         new Guid(connection1.id));

    var connection2 = FabricConnectionsApi.CreateSqlConnection(AppSettings.SqlServer, AppSettings.SqlDatabase);

    var connection3 = FabricConnectionsApi.CreateAnonymousWebConnection(AppSettings.WebUrlForData);

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void DeploySolutionWithImportedSalesModel(string WorkspaceName) {

    AppLogger.LogSolution("Deploy Solution with Imported Sales Model");

    var workspace = FabricRestApi.CreateWorkspace(WorkspaceName);

    var modelCreateRequest =  FabricItemDefinitionFactory.GetImportedSalesModelCreateRequest(ImportedModelName);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);

    // *** Use this workaround until Fabric Connections API is available
    PowerBiRestApi.PatchAnonymousAccessWebCredentials(workspace.Id, model.Id.Value);

    // *** Uncomment this code once Fabric Connections API is available
    // var url = PowerBiRestApi.GetWebDatasourceUrl(workspace.Id, model.Id.Value);
    // var connection = FabricConnectionsApi.CreateAnonymousWebConnection(url);
    // PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, new Guid(connection.id));

    PowerBiRestApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, ImportedModelName);

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);
  }

  public static void UpdateProductSalesSemanticModel(string WorkspaceName) {

    Workspace workspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);

    var model = FabricRestApi.GetSemanticModelByName(workspace.Id, ImportedModelName);

    AppLogger.LogOperationStart($"Updating item definition for semantic model named {model.DisplayName}");

    var updateModelRequest =
      FabricItemDefinitionFactory.GetImportedSalesModelUpdateRequest(ImportedModelName);

    FabricRestApi.UpdateItemDefinition(workspace.Id, model.Id.Value, updateModelRequest);

    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void UpdateProductSalesReport(string WorkspaceName) {

    Workspace workspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);

    var model = FabricRestApi.GetSemanticModelByName(workspace.Id, ImportedModelName);

    var report = FabricRestApi.GetReportByName(workspace.Id, ImportedModelName);

    AppLogger.LogOperationStart($"Updating item definition for report named {report.DisplayName}");

    var updateReportRequest =
      FabricItemDefinitionFactory.GetSalesReportUpdateRequest(model.Id.Value, ImportedModelName);

    FabricRestApi.UpdateItemDefinition(workspace.Id, report.Id.Value, updateReportRequest);

    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void CloneWorkspace(string SourceWorkspaceName, string CloneWorkspaceName) {

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(SourceWorkspaceName);

    var cloneWorkspace = FabricRestApi.CreateWorkspace(CloneWorkspaceName);

    // create dictionary to track Semantic Model Id mapping to rebind repots to correct cloned
    var semanticModelIdRedirects = new Dictionary<string, string>();

    // Enumerate through semantic models in source workspace to create semantic models in target workspace
    var sementicModels = FabricRestApi.GetItems(sourceWorkspace.Id, "SemanticModel");
    foreach (var sourceModel in sementicModels) {

      // get item definition from source Guid
      var sourceModelDefition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceModel.Id.Value);

      // use item definition to create clone in target workspace
      var createRequest = new CreateItemRequest(sourceModel.DisplayName, sourceModel.Type);
      createRequest.Definition = sourceModelDefition;
      var cloneModel = FabricRestApi.CreateItem(cloneWorkspace.Id, createRequest);

      // track mapping between source semantic model and target semantic model
      semanticModelIdRedirects.Add(sourceModel.Id.Value.ToString(), cloneModel.Id.Value.ToString());

    }

    // enumerate through semantic models to set credentials and refresh
    var pbiDatasets = PowerBiRestApi.GetDatasetsInWorkspace(cloneWorkspace.Id);
    foreach (var pbiDataset in pbiDatasets) {
      var datasources = PowerBiRestApi.GetDatasourcesForDataset(cloneWorkspace.Id.ToString(), pbiDataset.Id);
      foreach (var datasource in datasources) {
        if (datasource.DatasourceType == "Web") {
          AppLogger.LogStep("Patch credetials for web datasource");
          PowerBiRestApi.PatchAnonymousAccessWebCredentials(cloneWorkspace.Id, new Guid(pbiDataset.Id));
        }
      }
      PowerBiRestApi.RefreshDataset(cloneWorkspace.Id, new Guid(pbiDataset.Id));
    }

    var reports = FabricRestApi.GetItems(sourceWorkspace.Id, "Report");
    foreach (var sourceReport in reports) {

      // get item definition from source workspace
      var reportDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);

      // substitute Ids in definition.pbir to redirect to semantic model in clone workspace
      reportDefinition = FabricRestApi.UpdateItemDefinitionPart(reportDefinition,
                                                                "definition.pbir",
                                                                semanticModelIdRedirects);

      // prepare create item definition request used updated item definition
      var createRequest = new CreateItemRequest(sourceReport.DisplayName, sourceReport.Type);
      createRequest.Definition = reportDefinition;

      // created clone report
      var clonedNotebook = FabricRestApi.CreateItem(cloneWorkspace.Id, createRequest);

    }

    Console.WriteLine();
    Console.WriteLine("Workspace cloning process complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();

    OpenWorkspaceInBrowser(cloneWorkspace.Id);


  }

  public static void DeploySolutionWithLakehouseAndNotebook(string WorkspaceName) {

    string lakehouseName = "sales";
    string notebookName = "Create Lakehouse Tables";

    AppLogger.LogSolution("Deploy Solution with Lakehouse, Notebook and DirectLake Semantic Model");

    var workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    var lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);

    string codeContent = FabricIsvPlaybook.Properties.Resources.CreateLakehouseTables_ipynb;
    var notebookCreateRequest = FabricItemDefinitionFactory.GetNotebookCreateRequest(workspace.Id, lakehouse, notebookName, codeContent);

    var notebook = FabricRestApi.CreateItem(workspace.Id, notebookCreateRequest);

    FabricRestApi.RunNotebook(workspace.Id, notebook);

    AppLogger.LogOperationStart("Getting SQL endpoint connection information");
    var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    var modelCreateRequest =
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest(DirectLakeModelName, sqlEndpoint.ConnectionString, sqlEndpoint.Id);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);

    // *** Uncomment next two lines once Fabric Connections API is available
    // var sqlConnection = FabricConnectionsApi.CreateSqlEndpointConnectionUsingServicePrincipal(sqlEndpoint.ConnectionString, sqlEndpoint.Id);
    // PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, new Guid(sqlConnection.id));

    PowerBiRestApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, DirectLakeModelName);

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void DeploySolutionWithWarehouseAndDataPipelines(string WorkspaceName) {

    Console.WriteLine("Deploy Solution with Warehouse and DataPipelines");

    string warehouseName = "sales";
    string LakehouseName = "staging";

    var workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    var lakehouseItem = FabricRestApi.CreateLakehouse(workspace.Id, LakehouseName);

    var lakehouse = FabricRestApi.GetLakehouse(workspace.Id, lakehouseItem.Id.Value);

    string server = AppSettings.AzureStorageServer;
    string path = AppSettings.AzureStoragePath;
    string connectionName = $"{WorkspaceName}-{server}:{path}";

    var adlsConnection = FabricConnectionsApi.CreateAzureStorageConnection(server, path);

    // create and run data pipeline 1
    string dataPipelineName1 = "Copy Data Files To Staging Lakehouse";
    var createDataPipelineRequest1 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForLakehouse(
                                    dataPipelineName1,
                                    FabricIsvPlaybook.Properties.Resources.CopySalesData_json,
                                    workspace.Id.ToString(),
                                    lakehouse.Id.Value.ToString(),
                                    adlsConnection.id);

    var pipeline1 = FabricRestApi.CreateItem(workspace.Id, createDataPipelineRequest1);

    FabricRestApi.RunDataPipeline(workspace.Id, pipeline1);

    var warehouse = FabricRestApi.CreateWarehouse(workspace.Id, warehouseName);

    AppLogger.LogStep("Getting SQL endpoint connection string for data warehouse");
    string warehouseConnectString = FabricRestApi.GetSqlConnectionStringForWarehouse(workspace.Id, warehouse.Id.Value);



    // create and run data pipeline 2
    string dataPipeLineName2 = "Create Warehouse Tables";
    var createDataPipelineRequest2 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName2,
                                    FabricIsvPlaybook.Properties.Resources.CreateWarehouseTables_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline2 = FabricRestApi.CreateItem(workspace.Id, createDataPipelineRequest2);

    FabricRestApi.RunDataPipeline(workspace.Id, pipeline2);

    // create and run data pipeline 3
    string dataPipeLineName3 = "Create Warehouse Stored Procedures";
    var createDataPipelineRequest3 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName3,
                                    FabricIsvPlaybook.Properties.Resources.CreateStoredProcedures_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline3 = FabricRestApi.CreateItem(workspace.Id, createDataPipelineRequest3);

    FabricRestApi.RunDataPipeline(workspace.Id, pipeline3);


    // create and run data pipeline 4
    string dataPipeLineName4 = "Refresh All Warehouse Tables";
    var createDataPipelineRequest4 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName4,
                                    FabricIsvPlaybook.Properties.Resources.RefreshAllTables_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline4 = FabricRestApi.CreateItem(workspace.Id, createDataPipelineRequest4);

    FabricRestApi.RunDataPipeline(workspace.Id, pipeline4);


    var modelCreateRequest =
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest("Product Sales", warehouseConnectString, warehouseName);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);

    // *** Uncomment next two lines once Fabric Connections API is available
    // var sqlConnection = FabricConnectionsApi.CreateSqlEndpointConnectionUsingServicePrincipal(sqlEndpoint.ConnectionString, sqlEndpoint.Id);
    // PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, new Guid(sqlConnection.id));

    PowerBiRestApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, "Product Sales");

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void DeploySolutionWithWithShortcutAndLoadTableApi(string WorkspaceName) {

    string warehouseName = "sales";
    string lakehouseName = "staging";

    AppLogger.LogSolution("Deploy Solution with OneLake Shortcut connected to ADLS Gen2 storage");

    Workspace workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    Item lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);

    string path = "Files";
    Uri location = new Uri(AppSettings.AzureStorageServer);
    string shortcutSubpath = "sales-data";

    var connection = FabricConnectionsApi.CreateAzureStorageConnection(AppSettings.AzureStorageServer,
                                                                       AppSettings.AzureStoragePath);

    FabricRestApi.CreateAdlsGen2Shortcut(workspace.Id, lakehouse.Id.Value, location, path, shortcutSubpath, new Guid(connection.id));

    FabricRestApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/customers.csv", "customers");
    FabricRestApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/invoices.csv", "invoices");
    FabricRestApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/invoice_details.csv", "invoice_details");
    FabricRestApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/products.csv", "products");

    var warehouse = FabricRestApi.CreateWarehouse(workspace.Id, warehouseName);

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for data warehouse");
    string warehouseConnectString = FabricRestApi.GetSqlConnectionStringForWarehouse(workspace.Id, warehouse.Id.Value);
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

    var pipeline2 = FabricRestApi.CreateItem(workspace.Id, createDataPipelineRequest2);

    FabricRestApi.RunDataPipeline(workspace.Id, pipeline2);

    // create and run data pipeline 3
    string dataPipeLineName3 = "Create Warehouse Stored Procedures";
    var createDataPipelineRequest3 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName3,
                                    FabricIsvPlaybook.Properties.Resources.CreateStoredProcedures_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline3 = FabricRestApi.CreateItem(workspace.Id, createDataPipelineRequest3);

    FabricRestApi.RunDataPipeline(workspace.Id, pipeline3);


    // create and run data pipeline 4
    string dataPipeLineName4 = "Refresh All Warehouse Tables";
    var createDataPipelineRequest4 =
      FabricItemDefinitionFactory.GetDataPipelineCreateRequestForWarehouse(
                                    dataPipeLineName4,
                                    FabricIsvPlaybook.Properties.Resources.RefreshAllTables_json,
                                    workspace.Id.ToString(),
                                    warehouse.Id.Value.ToString(),
                                    warehouseConnectString);

    var pipeline4 = FabricRestApi.CreateItem(workspace.Id, createDataPipelineRequest4);

    FabricRestApi.RunDataPipeline(workspace.Id, pipeline4);


    var modelCreateRequest =
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest("Product Sales", warehouseConnectString, warehouseName);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);

    // *** Uncomment next two lines once Fabric Connections API is available
    // var sqlConnection = FabricConnectionsApi.CreateSqlEndpointConnectionUsingServicePrincipal(sqlEndpoint.ConnectionString, sqlEndpoint.Id);
    // PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, new Guid(sqlConnection.id));

    PowerBiRestApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, "Product Sales");

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

    OpenWorkspaceInBrowser(workspace.Id);

  }
 
  public static void DeploySolutionWithWarehouseAndSqlClient(string WorkspaceName) {

    string warehouseName = "sales";
    string lakehouseName = "staging";

    AppLogger.LogSolution("Deploy Solution with Warehouse and Microsoft.Data.SqlClient");

    Workspace workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    Item lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);

    string path = "Files";
    string subpath = "sales-data";
    Uri location = new Uri(AppSettings.AzureStorageServer);

    AppLogger.LogStep($"Create Connection to ADLS Gen2 Storage path {AppSettings.AzureStorageServer}");

    var adlsConnection = FabricConnectionsApi.CreateAzureStorageConnection(AppSettings.AzureStorageServer,
                                                                       AppSettings.AzureStoragePath);

    AppLogger.LogStep($"Create Shortcut to ADLS Gen2 Storage with path {AppSettings.AzureStorageServer}");
    FabricRestApi.CreateAdlsGen2Shortcut(workspace.Id,
                                         lakehouse.Id.Value,
                                         location,
                                         path,
                                         subpath,
                                         new Guid(adlsConnection.id));


    AppLogger.LogStep("Loading delta tables in lakehouse from data files using Fabric Load Table API");

    AppLogger.LogSubstep("Loading table from Files/sales-data/products.csv");
    FabricRestApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/products.csv", "products");

    AppLogger.LogSubstep("Loading table from Files/sales-data/customers.csv");
    FabricRestApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/customers.csv", "customers");

    AppLogger.LogSubstep("Loading table from Files/sales-data/invoices.csv");
    FabricRestApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/invoices.csv", "invoices");

    AppLogger.LogSubstep("Loading table from Files/sales-data/invoice_details.csv");
    FabricRestApi.LoadLakehouseTableFromCsv(workspace.Id, lakehouse.Id.Value, "Files/sales-data/invoice_details.csv", "invoice_details");

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for lakehouse");
    var sqlEndpointLakehouse = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {sqlEndpointLakehouse.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpointLakehouse.Id);

    var warehouseItem = FabricRestApi.CreateWarehouse(workspace.Id, warehouseName);

    AppLogger.LogOperationStart("Getting SQL endpoint connection information for data warehouse");
    string warehouseServer = FabricRestApi.GetSqlConnectionStringForWarehouse(workspace.Id, warehouseItem.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"Server: {warehouseServer}");
    AppLogger.LogSubstep($"Database: {warehouseItem.Id.ToString()}");

    AppLogger.LogStep("Connecting to warehouse SQL Endpoint to execute SQL commands");
    var sqlWriter = new SqlConnectionWriter(warehouseServer, warehouseName);

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
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest("Product Sales", warehouseServer, warehouseName);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);

    // *** Uncomment next two lines once Fabric Connections API is available
    // var sqlConnection = FabricConnectionsApi.CreateSqlEndpointConnectionUsingServicePrincipal(sqlEndpoint.ConnectionString, sqlEndpoint.Id);
    // PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, new Guid(sqlConnection.id));

    PowerBiRestApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, "Product Sales");

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void DeploySolutionWithAdlsGen2Api(string WorkspaceName) {

    string LakehouseName = "staging";
    string warehouseName = "sales";

    AppLogger.LogSolution("Deploy Solution with using ADLS Gen2 API and LoadTable API");

    var workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.PremiumCapacityId);

    var lakehouseItem = FabricRestApi.CreateLakehouse(workspace.Id, LakehouseName);

    var lakehouse = FabricRestApi.GetLakehouse(workspace.Id, lakehouseItem.Id.Value);

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
      FabricRestApi.LoadLakehouseTableFromParquet(workspace.Id, lakehouse.Id.Value, "Files/landing_zone/" + fileName, tableName);
    }

    var warehouseItem = FabricRestApi.CreateWarehouse(workspace.Id, warehouseName);

    AppLogger.LogStep("Getting SQL connection string  for data warehouse");
    string warehouseServer = FabricRestApi.GetSqlConnectionStringForWarehouse(workspace.Id, warehouseItem.Id.Value);

    AppLogger.LogSubstep($"Server: {warehouseServer}");
    AppLogger.LogSubstep($"Database: {warehouseItem.Id.ToString()}");

    AppLogger.LogStep("Connecting to warehouse SQL Endpoint to execute SQL commands");
    var sqlWriter = new SqlConnectionWriter(warehouseServer, warehouseName);

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
      FabricItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest("Product Sales", warehouseServer, warehouseName);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);

    // *** Uncomment next two lines once Fabric Connections API is available
    // var sqlConnection = FabricConnectionsApi.CreateSqlEndpointConnectionUsingServicePrincipal(sqlEndpoint.ConnectionString, sqlEndpoint.Id);
    // PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, new Guid(sqlConnection.id));

    PowerBiRestApi.RefreshDataset(workspace.Id, model.Id.Value);

    var createRequestReport =
      FabricItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, "Product Sales");

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogStep("Customer tenant provisioning complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void BranchOutToFeatureWorkspace(string WorkspaceName, string FeatureName) {

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);

    var featureWorkspace = FabricRestApi.CreateWorkspace(WorkspaceName + " - " + FeatureName);

    var lakehouseNames = new List<string>();
    var lakehouseIdRedirects = new Dictionary<string, string>();
    var lakehouseSqlEndpointRedirects = new Dictionary<string, string>();

    lakehouseIdRedirects.Add(sourceWorkspace.Id.ToString(), featureWorkspace.Id.ToString());

    // Enumerate through semantic models in source workspace to create semantic models in target workspace
    var lakehouses = FabricRestApi.GetItems(sourceWorkspace.Id, "Lakehouse");

    foreach (var sourceLakehouse in lakehouses) {

      var sourceLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(sourceWorkspace.Id, sourceLakehouse.Id.Value);

      var newLakehouse = FabricRestApi.CreateLakehouse(featureWorkspace.Id, sourceLakehouse.DisplayName);

      var newLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(featureWorkspace.Id, newLakehouse.Id.Value);

      lakehouseNames.Add(sourceLakehouse.DisplayName);
      lakehouseIdRedirects.Add(sourceLakehouse.Id.Value.ToString(), newLakehouse.Id.Value.ToString());
      lakehouseSqlEndpointRedirects.Add(sourceLakehouseSqlEndpoint.ConnectionString, newLakehouseSqlEndpoint.ConnectionString);
      lakehouseSqlEndpointRedirects.Add(sourceLakehouseSqlEndpoint.Id, newLakehouseSqlEndpoint.Id);
    }

    var notebooks = FabricRestApi.GetItems(sourceWorkspace.Id, "Notebook");
    foreach (var sourceNotebook in notebooks) {

      // get item definition from source Guid
      var notebookDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceNotebook.Id.Value);
      notebookDefinition = FabricRestApi.UpdateItemDefinitionPart(notebookDefinition, "notebook-content.py", lakehouseIdRedirects);

      // use item definition to create clone in target workspace
      var createRequest = new CreateItemRequest(sourceNotebook.DisplayName, sourceNotebook.Type);
      createRequest.Definition = notebookDefinition;
      var clonedNotebook = FabricRestApi.CreateItem(featureWorkspace.Id, createRequest);

      FabricRestApi.RunNotebook(featureWorkspace.Id, clonedNotebook);

    }

    // create dictionary to track Semantic Model Id mapping to rebind repots to correct cloned
    var semanticModelRedirects = new Dictionary<string, string>();

    // Enumerate through semantic models in source workspace to create semantic models in target workspace
    var sementicModels = FabricRestApi.GetItems(sourceWorkspace.Id, "SemanticModel");
    foreach (var sourceModel in sementicModels) {

      // ignore default semantic model for lakehouse
      if (!lakehouseNames.Contains(sourceModel.DisplayName)) {

        // get item definition from source item
        var sementicModelDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceModel.Id.Value, "TMDL");

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        sementicModelDefinition = FabricRestApi.UpdateItemDefinitionPart(sementicModelDefinition, "definition/expressions.tmdl", lakehouseSqlEndpointRedirects);

        // use item definition to create clone in target workspace
        var createRequest = new CreateItemRequest(sourceModel.DisplayName, sourceModel.Type);
        createRequest.Definition = sementicModelDefinition;
        var clonedModel = FabricRestApi.CreateItem(featureWorkspace.Id, createRequest);

        // track mapping between source semantic model and target semantic model
        semanticModelRedirects.Add(sourceModel.Id.Value.ToString(), clonedModel.Id.Value.ToString());

      }
    }

    var reports = FabricRestApi.GetItems(sourceWorkspace.Id, "Report");
    foreach (var sourceReport in reports) {

      // get item definition from source workspace
      var itemDef = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);

      var reportDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);
      reportDefinition = FabricRestApi.UpdateItemDefinitionPart(reportDefinition, "definition.pbir", semanticModelRedirects);

      // use item definition to create clone in target workspace
      var createRequest = new CreateItemRequest(sourceReport.DisplayName, sourceReport.Type);
      createRequest.Definition = reportDefinition;
      var clonedNotebook = FabricRestApi.CreateItem(featureWorkspace.Id, createRequest);


    }

    Console.WriteLine();
    Console.WriteLine("Customer tenant provisioning complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();

    OpenWorkspaceInBrowser(featureWorkspace.Id);


  }

  public static void BranchOutToFeatureWorkspaceShallowCopy(string WorkspaceName, string FeatureName) {

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);

    var featureWorkspace = FabricRestApi.CreateWorkspace(WorkspaceName + " - " + FeatureName);

    // Enumerate through semantic models in source workspace to create semantic models in target workspace
    var lakehouses = FabricRestApi.GetItems(sourceWorkspace.Id, "Lakehouse");

    foreach (var sourceLakehouse in lakehouses) {

      var sourceLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(sourceWorkspace.Id, sourceLakehouse.Id.Value);

      var newLakehouse = FabricRestApi.CreateLakehouse(featureWorkspace.Id, sourceLakehouse.DisplayName);
      var newLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(featureWorkspace.Id, newLakehouse.Id.Value);

    }

    var notebooks = FabricRestApi.GetItems(sourceWorkspace.Id, "Notebook");
    foreach (var sourceNotebook in notebooks) {

      // get item definition from source Guid
      var notebookDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceNotebook.Id.Value);

      // use item definition to create clone in target workspace
      var createRequest = new CreateItemRequest(sourceNotebook.DisplayName, sourceNotebook.Type);
      createRequest.Definition = notebookDefinition;
      var clonedNotebook = FabricRestApi.CreateItem(featureWorkspace.Id, createRequest);

      FabricRestApi.RunNotebook(featureWorkspace.Id, clonedNotebook);

    }

    // create dictionary to track Semantic Model Id mapping to rebind repots to correct cloned
    var semanticModelRedirects = new Dictionary<string, string>();

    // Enumerate through semantic models in source workspace to create semantic models in target workspace
    var sementicModels = FabricRestApi.GetItems(sourceWorkspace.Id, "SemanticModel");
    foreach (var sourceModel in sementicModels) {


      // get item definition from source Guid
      var sementicModelDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceModel.Id.Value, "TMSL");

      // use item definition to create clone in target workspace
      var createRequest = new CreateItemRequest(sourceModel.DisplayName, sourceModel.Type);
      createRequest.Definition = sementicModelDefinition;
      var clonedModel = FabricRestApi.CreateItem(featureWorkspace.Id, createRequest);

      // track mapping between source semantic model and target semantic model
      semanticModelRedirects.Add(sourceModel.Id.Value.ToString(), clonedModel.Id.Value.ToString());


    }

    var reports = FabricRestApi.GetItems(sourceWorkspace.Id, "Report");
    foreach (var sourceReport in reports) {

      // get item definition from source workspace
      var itemDef = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);

      var reportDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);

      // use item definition to create clone in target workspace
      var createRequest = new CreateItemRequest(sourceReport.DisplayName, sourceReport.Type);
      createRequest.Definition = reportDefinition;
      var clonedNotebook = FabricRestApi.CreateItem(featureWorkspace.Id, createRequest);

    }

    Console.WriteLine();
    Console.WriteLine("Customer tenant provisioning complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();

    OpenWorkspaceInBrowser(featureWorkspace.Id);
  }

  public static void ExportItemDefinitionsFromWorkspace(string WorkspaceName) {
    FabricRestApi.ExportItemDefinitionsFromWorkspace(WorkspaceName);
  }

  private static void OpenWorkspaceInBrowser(Guid WorkspaceId) {
    OpenWorkspaceInBrowser(WorkspaceId.ToString());
  }

  private static void OpenWorkspaceInBrowser(string WorkspaceId) {

    string url = "https://app.powerbi.com/groups/" + WorkspaceId;

    var process = new Process();
    process.StartInfo = new ProcessStartInfo(@"C:\Program Files\Google\Chrome\Application\chrome.exe");
    process.StartInfo.Arguments = url + " --profile-directory=\"Profile 1\" ";
    process.Start();

  }

}
