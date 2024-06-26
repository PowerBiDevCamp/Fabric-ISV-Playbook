using Microsoft.Fabric;
using FabricAdmin = Microsoft.Fabric.Api.Admin.Models;
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Notebook.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;
using Microsoft.Fabric.Api.Warehouse.Models;
using Microsoft.Fabric.Api.SemanticModel.Models;
using Microsoft.Fabric.Api.Report.Models;
using Microsoft.Fabric.Api.Utils;

using System.Text.Json;


public class FabricUserApi {

  private static string accessToken;
  private static FabricClient fabricApiClient;

  static FabricUserApi() {
    accessToken = EntraIdTokenManager.GetAccessToken(FabricPermissionScopes.Fabric_User_Impresonation);
    fabricApiClient = new FabricClient(accessToken);
  }

  public static List<Workspace> GetWorkspaces() {
    return fabricApiClient.Core.Workspaces.ListWorkspaces().ToList();
  }

  public static List<Capacity> GetCapacities() {
    return fabricApiClient.Core.Capacities.ListCapacities().ToList();
  }

  public static Workspace GetWorkspaceByName(string WorkspaceName) {
    var workspaces = fabricApiClient.Core.Workspaces.ListWorkspaces().ToList();

    foreach (var workspace in workspaces) {
      if (workspace.DisplayName.Equals(WorkspaceName)) {
        return workspace;
      }
    }

    return null;
  }

  public static Workspace CreateWorkspace(string WorkspaceName, string CapacityId = AppSettings.PremiumCapacityId, string Description = null) {

    var workspace = GetWorkspaceByName(WorkspaceName);

    AppLogger.LogOperationStart("Creating workspace named " + WorkspaceName);

    // delete workspace with same name if it exists
    if (workspace != null) {
      DeleteWorkspace(workspace.Id);
      workspace = null;
    }

    var createRequest = new CreateWorkspaceRequest(WorkspaceName);
    createRequest.Description = Description;

    workspace = fabricApiClient.Core.Workspaces.CreateWorkspace(createRequest);

    AppLogger.LogOperationComplete();
    AppLogger.LogSubstep($"Workspace created with id of {workspace.Id}");

    if (CapacityId != null) {
      var capacityId = new Guid(CapacityId);
      AssignWorkspaceToCapacity(workspace.Id, capacityId);
      AppLogger.LogSubstep($"Workspace associated with capacity with Id {CapacityId}");
    }

    return workspace;
  }

  public static Workspace UpdateWorkspace(Guid WorkspaceId, string WorkspaceName, string Description = null) {

    var updateRequest = new UpdateWorkspaceRequest {
      DisplayName = WorkspaceName,
      Description = Description
    };

    return fabricApiClient.Core.Workspaces.UpdateWorkspace(WorkspaceId, updateRequest).Value;
  }

  public static void DeleteWorkspace(Guid WorkspaceId) {
    fabricApiClient.Core.Workspaces.DeleteWorkspace(WorkspaceId);
  }

  public static void AssignWorkspaceToCapacity(Guid WorkspaceId, Guid CapacityId) {
    var assignRequest = new AssignWorkspaceToCapacityRequest(CapacityId);
    fabricApiClient.Core.Workspaces.AssignToCapacity(WorkspaceId, assignRequest);
  }

  public static void AddUserAsWorkspaceMemeber(Guid WorkspaceId, Guid UserId, WorkspaceRole RoleAssignment) {
    var user = new Principal(UserId, PrincipalType.User);
    var roleAssignment = new AddWorkspaceRoleAssignmentRequest(user, RoleAssignment);
    fabricApiClient.Core.Workspaces.AddWorkspaceRoleAssignment(WorkspaceId, roleAssignment);
  }

  public static void AddGroupAsWorkspaceMemeber(Guid WorkspaceId, Guid GroupId, WorkspaceRole RoleAssignment) {
    var group = new Principal(GroupId, PrincipalType.Group);
    var roleAssignment = new AddWorkspaceRoleAssignmentRequest(group, RoleAssignment);
    fabricApiClient.Core.Workspaces.AddWorkspaceRoleAssignment(WorkspaceId, roleAssignment);
  }

  public static void AddServicePrincipalAsWorkspaceMemeber(Guid WorkspaceId, Guid ServicePrincipalObjectId, WorkspaceRole RoleAssignment) {
    var user = new Principal(ServicePrincipalObjectId, PrincipalType.ServicePrincipal);
    var roleAssignment = new AddWorkspaceRoleAssignmentRequest(user, RoleAssignment);
    fabricApiClient.Core.Workspaces.AddWorkspaceRoleAssignment(WorkspaceId, roleAssignment);
  }

  public static void ViewWorkspaceRoleAssignments(Guid WorkspaceId) {

    var roleAssignments = fabricApiClient.Core.Workspaces.ListWorkspaceRoleAssignments(WorkspaceId);

    AppLogger.LogStep("Viewing workspace reole assignments");
    foreach (var roleAssignment in roleAssignments) {
      AppLogger.LogSubstep($"{roleAssignment.Principal.DisplayName} ({roleAssignment.Principal.Type}) added in role of {roleAssignment.Role}");
    }

  }

  public static Item CreateItem(Guid WorkspaceId, CreateItemRequest CreateRequest) {

    AppLogger.LogOperationStart($"Creating {CreateRequest.Type} named {CreateRequest.DisplayName}");

    var newItem = fabricApiClient.Core.Items.CreateItemAsync(WorkspaceId,
                                                             CreateRequest).Result.Value;

    AppLogger.LogOperationComplete();

    AppLogger.LogSubstep($"{newItem.Type} created with Id {newItem.Id}");

    // return new item object to caller
    return newItem;
  }

  public static Item UpdateItem(Guid WorkspaceId, Guid ItemId, string ItemName, string Description = null) {

    var updateRequest = new UpdateItemRequest {
      DisplayName = ItemName,
      Description = Description
    };

    var item = fabricApiClient.Core.Items.UpdateItem(WorkspaceId, ItemId, updateRequest).Value;

    return item;

  }

  public static List<Item> GetWorkspaceItems(Guid WorkspaceId, string ItemType = null) {
    return fabricApiClient.Core.Items.ListItems(WorkspaceId, ItemType).ToList();
  }

  public static ItemDefinition GetItemDefinition(Guid WorkspaceId, Guid ItemId, string Format = null) {
    var response = fabricApiClient.Core.Items.GetItemDefinitionAsync(WorkspaceId, ItemId, Format).Result.Value;
    return response.Definition;
  }

  public static void ExportItemDefinitionsFromWorkspace(string WorkspaceName) {

    AppLogger.LogStep($"Exporting workspaces items from {WorkspaceName}");

    FabricItemDefinitionFactory.DeleteAllTemplateFiles(WorkspaceName);

    var workspace = GetWorkspaceByName(WorkspaceName);
    var items = GetWorkspaceItems(workspace.Id);

    // list of items types that do not support getItemDefinition
    List<ItemType> unsupportedItems = new List<ItemType>() {
      ItemType.Lakehouse,
      ItemType.SQLEndpoint,
      ItemType.Warehouse,
      ItemType.Dashboard,
      ItemType.Datamart,
      ItemType.PaginatedReport
    };

    foreach (var item in items) {
      if (!unsupportedItems.Contains(item.Type)) {
        try {
          string format = (item.Type == ItemType.Notebook) ? "ipynb" : null;
          var definition = GetItemDefinition(workspace.Id, item.Id.Value, format);

          string targetFolder = item.DisplayName + "." + item.Type;

          AppLogger.LogSubstep($"Exporting {targetFolder}");

          foreach (var part in definition.Parts) {
            FabricItemDefinitionFactory.WriteFile(WorkspaceName, targetFolder, part.Path, part.Payload);
          }

        }
        catch (Exception ex) {
          AppLogger.LogException(ex);
        }
        // slow up calls so it doesn't trigger throttleing for more than 10+ calls per minute
        Thread.Sleep(7000);
      }
    }

    AppLogger.LogSubstep("Exporting process completed");

  }

  public static void UpdateItemDefinition(Guid WorkspaceId, Guid ItemId, UpdateItemDefinitionRequest UpdateRequest) {
    fabricApiClient.Core.Items.UpdateItemDefinition(WorkspaceId, ItemId, UpdateRequest);
  }

  public static SemanticModel GetSemanticModelByName(Guid WorkspaceId, string Name) {
    var models = fabricApiClient.SemanticModel.Items.ListSemanticModels(WorkspaceId);
    foreach (var model in models) {
      if (Name == model.DisplayName) {
        return model;
      }
    }
    return null;
  }

  public static Report GetReportByName(Guid WorkspaceId, string Name) {
    var reports = fabricApiClient.Report.Items.ListReports(WorkspaceId);
    foreach (var report in reports) {
      if (Name == report.DisplayName) {
        return report;
      }
    }
    return null;
  }

  public static Item CreateLakehouse(Guid WorkspaceId, string LakehouseName) {

    // Item create request for lakehouse des not include item definition
    var createRequest = new CreateItemRequest(LakehouseName, ItemType.Lakehouse);

    // create lakehouse
    return CreateItem(WorkspaceId, createRequest);
  }

  public static Shortcut CreateLakehouseShortcut(Guid WorkspaceId, Guid LakehouseId, CreateShortcutRequest CreateShortcutRequest) {
    return fabricApiClient.Core.OneLakeShortcuts.CreateShortcut(WorkspaceId, LakehouseId, CreateShortcutRequest).Value;
  }

  public static Lakehouse GetLakehouse(Guid WorkspaceId, Guid LakehousId) {
    return fabricApiClient.Lakehouse.Items.GetLakehouse(WorkspaceId, LakehousId).Value;
  }

  public static Lakehouse GetLakehouseByName(Guid WorkspaceId, string LakehouseName) {

    var lakehouses = fabricApiClient.Lakehouse.Items.ListLakehouses(WorkspaceId);

    foreach (var lakehouse in lakehouses) {
      if (lakehouse.DisplayName == LakehouseName) {
        return lakehouse;
      }
    }

    return null;
  }

  public static Notebook GetNotebookByName(Guid WorkspaceId, string NotebookName) {

    var notebooks = fabricApiClient.Notebook.Items.ListNotebooks(WorkspaceId);

    foreach (var notebook in notebooks) {
      if (notebook.DisplayName == NotebookName) {
        return notebook;
      }
    }

    return null;
  }


  public static SqlEndpointProperties GetSqlEndpointForLakehouse(Guid WorkspaceId, Guid LakehouseId) {

    var lakehouse = GetLakehouse(WorkspaceId, LakehouseId);

    while (lakehouse.Properties.SqlEndpointProperties.ProvisioningStatus != "Success") {
      lakehouse = GetLakehouse(WorkspaceId, LakehouseId);
      Thread.Sleep(10000); // wait 10 seconds
      AppLogger.LogOperationInProgress();
    }

    return lakehouse.Properties.SqlEndpointProperties;

  }

  public static Item CreateWarehouse(Guid WorkspaceId, string WarehouseName) {

    // Item create request for lakehouse des not include item definition
    var createRequest = new CreateItemRequest(WarehouseName, ItemType.Warehouse);

    // create lakehouse
    return CreateItem(WorkspaceId, createRequest);
  }

  public static Warehouse GetWareHouseByName(Guid WorkspaceId, string WarehouseName) {

    var warehouses = fabricApiClient.Warehouse.Items.ListWarehouses(WorkspaceId);

    foreach (var warehouse in warehouses) {
      if (warehouse.DisplayName == WarehouseName) {
        return warehouse;
      }
    }

    return null;
  }

  public static Warehouse GetWarehouse(Guid WorkspaceId, Guid WarehouseId) {
    return fabricApiClient.Warehouse.Items.GetWarehouse(WorkspaceId, WarehouseId).Value;
  }

  public static void LoadLakehouseTableFromParquet(Guid WorkspaceId, Guid LakehouseId, string SourceFile, string TableName) {

    var loadTableRequest = new LoadTableRequest(SourceFile, PathType.File);
    loadTableRequest.Recursive = false;
    loadTableRequest.Mode = ModeType.Overwrite;
    loadTableRequest.FormatOptions = new Parquet();

    fabricApiClient.Lakehouse.Tables.LoadTableAsync(WorkspaceId, LakehouseId, TableName, loadTableRequest).Wait();

  }

  public static void LoadLakehouseTableFromCsv(Guid WorkspaceId, Guid LakehouseId, string SourceFile, string TableName) {

    var loadTableRequest = new LoadTableRequest(SourceFile, PathType.File);
    loadTableRequest.Recursive = false;
    loadTableRequest.Mode = ModeType.Overwrite;
    loadTableRequest.FormatOptions = new Csv();

    fabricApiClient.Lakehouse.Tables.LoadTableAsync(WorkspaceId, LakehouseId, TableName, loadTableRequest).Wait();
  }

  public static void RunNotebook(Guid WorkspaceId, Item Notebook, RunOnDemandItemJobRequest JobRequest = null) {

    AppLogger.LogOperationStart($"Running Notebook named {Notebook.DisplayName}");

    var response = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, Notebook.Id.Value, "RunNotebook", JobRequest);

    if (response.Status == 202) {

      AppLogger.LogOperationInProgress();

      string location = response.GetLocationHeader();
      int? retryAfter = 5; // response.GetRetryAfterHeader();     
      Guid JobInstanceId = new Guid(location.Substring(location.LastIndexOf("/") + 1));

      Thread.Sleep(retryAfter.Value * 1000);

      var jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Notebook.Id.Value, JobInstanceId).Value;


      while (jobInstance.Status == Status.NotStarted || jobInstance.Status == Status.InProgress) {
        AppLogger.LogOperationInProgress();
        Thread.Sleep(retryAfter.Value * 1000);
        jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Notebook.Id.Value, JobInstanceId).Value;
      }

      AppLogger.LogOperationComplete();

      if (jobInstance.Status == Status.Completed) {
        AppLogger.LogSubstep("Notebook execution completed");
      }

      if (jobInstance.Status == Status.Failed) {
        AppLogger.LogSubstep("Notebook execution failed");
        AppLogger.LogSubstep(jobInstance.FailureReason.Message);
      }

      if (jobInstance.Status == Status.Cancelled) {
        AppLogger.LogSubstep("Notebook execution cancelled");
      }

      if (jobInstance.Status == Status.Deduped) {
        AppLogger.LogSubstep("Notebook execution deduped");
      }
    }
    else {
      AppLogger.LogStep("Notebook execution failed when starting");
    }

  }

  public static void RunTableMaintananceOperation(Guid WorkspaceId, Guid LakehouseId, string TableName) {

    var jobRequest = JobRequestUtils.GetJobRequestForTableMaintanance(TableName);

    var response = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, LakehouseId, "TableMaintenance", jobRequest);

    if (response.Status == 202) {

      AppLogger.LogOperationInProgress();

      string location = response.GetLocationHeader();
      int? retryAfter = 5; // response.GetRetryAfterHeader();     
      Guid JobInstanceId = new Guid(location.Substring(location.LastIndexOf("/") + 1));

      Thread.Sleep(retryAfter.Value * 1000);

      var jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, LakehouseId, JobInstanceId).Value;


      while (jobInstance.Status == Status.NotStarted || jobInstance.Status == Status.InProgress) {
        AppLogger.LogOperationInProgress();
        Thread.Sleep(retryAfter.Value * 1000);
        jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, LakehouseId, JobInstanceId).Value;
      }

      AppLogger.LogOperationComplete();

      if (jobInstance.Status == Status.Completed) {
        AppLogger.LogSubstep("Notebook execution completed");
      }

      if (jobInstance.Status == Status.Failed) {
        AppLogger.LogSubstep("Notebook execution failed");
        AppLogger.LogSubstep(jobInstance.FailureReason.Message);
      }

      if (jobInstance.Status == Status.Cancelled) {
        AppLogger.LogSubstep("Notebook execution cancelled");
      }

      if (jobInstance.Status == Status.Deduped) {
        AppLogger.LogSubstep("Notebook execution deduped");
      }
    }
    else {
      AppLogger.LogStep("Notebook execution failed when starting");
    }

  }

  public static void RunDataPipeline(Guid WorkspaceId, Item DataPipeline) {

    AppLogger.LogOperationStart($"Running DataPipeline named {DataPipeline.DisplayName}");

    var response = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, DataPipeline.Id.Value, "Pipeline");

    if (response.Status == 202) {

      AppLogger.LogOperationInProgress();

      string location = response.GetLocationHeader();
      int? retryAfter = response.GetRetryAfterHeader();     
      Guid JobInstanceId = new Guid(location.Substring(location.LastIndexOf("/") + 1));

      Thread.Sleep(retryAfter.Value * 1000);

      var jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, DataPipeline.Id.Value, JobInstanceId).Value;

      while (jobInstance.Status == Status.NotStarted || jobInstance.Status == Status.InProgress) {
        AppLogger.LogOperationInProgress();
        Thread.Sleep(retryAfter.Value * 1000);
        jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, DataPipeline.Id.Value, JobInstanceId).Value;
      }

      AppLogger.LogOperationComplete();

      if (jobInstance.Status == Status.Completed) {
        AppLogger.LogSubstep("Data pipeline execution completed");
      }

      if (jobInstance.Status == Status.Failed) {
        AppLogger.LogSubstep("Data pipeline execution failed");
        AppLogger.LogSubstep(jobInstance.FailureReason.Message);
      }

      if (jobInstance.Status == Status.Cancelled) {
        AppLogger.LogSubstep("Data pipeline execution cancelled");
      }

      if (jobInstance.Status == Status.Deduped) {
        AppLogger.LogSubstep("Data pipeline execution deduped");
      }
    }
    else {
      AppLogger.LogStep("Data pipeline execution failed when starting");
    }
  }



  // listings - do not keep

  public static void RunNotebookWithParameters(Guid WorkspaceId, Item Notebook) {

    var JobRequest = new RunOnDemandItemJobRequest {
      ExecutionData = new List<KeyValuePair<string, object>>() {
        new KeyValuePair<string, object>("parameters", new List<KeyValuePair<string, object>>(){
          new KeyValuePair<string, object>("fileName", new List<KeyValuePair<string, object>>() {
            new KeyValuePair<string, object>("value", "testfile.txt"),
            new KeyValuePair<string, object>("type", "string")
          }),
          new KeyValuePair<string, object>("fileContent", new List<KeyValuePair<string, object>>() {
            new KeyValuePair<string, object>("value", "Hello world"),
            new KeyValuePair<string, object>("type", "string")
          })
        })
      }
    };

    var response = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, Notebook.Id.Value, "RunNotebook", JobRequest);

    if (response.Status == 202) {

      AppLogger.LogOperationInProgress();

      string location = response.GetLocationHeader();
      int? retryAfter = 5; // response.GetRetryAfterHeader();     
      Guid JobInstanceId = new Guid(location.Substring(location.LastIndexOf("/") + 1));

      Thread.Sleep(retryAfter.Value * 1000);

      var jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Notebook.Id.Value, JobInstanceId).Value;


      while (jobInstance.Status == Status.NotStarted || jobInstance.Status == Status.InProgress) {
        AppLogger.LogOperationInProgress();
        Thread.Sleep(retryAfter.Value * 1000);
        jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Notebook.Id.Value, JobInstanceId).Value;
      }

      AppLogger.LogOperationComplete();

      if (jobInstance.Status == Status.Completed) {
        AppLogger.LogSubstep("Notebook execution completed");
      }

      if (jobInstance.Status == Status.Failed) {
        AppLogger.LogSubstep("Notebook execution failed");
        AppLogger.LogSubstep(jobInstance.FailureReason.Message);
      }

      if (jobInstance.Status == Status.Cancelled) {
        AppLogger.LogSubstep("Notebook execution cancelled");
      }

      if (jobInstance.Status == Status.Deduped) {
        AppLogger.LogSubstep("Notebook execution deduped");
      }
    }
    else {
      AppLogger.LogStep("Notebook execution failed when starting");
    }

  }


  public static void RunTableMaintananceJob(Guid WorkspaceId, Item Notebook, string TableName) {

    var jobRequest = new RunOnDemandItemJobRequest {
      ExecutionData = new List<KeyValuePair<string, object>>() {
          new KeyValuePair<string, object>("tableName", TableName),
          new KeyValuePair<string, object>("optimizeSettings", new List<KeyValuePair<string, object>>() {
            new KeyValuePair<string, object>("vOrder","true")
          }),
          new KeyValuePair<string, object>("vacuumSettings", new List<KeyValuePair<string, object>>() {
            new KeyValuePair<string, object>("retentionPeriod","7.01:00:00")
          })
        }
    };

    var response = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, Notebook.Id.Value, "TableMaintenance", jobRequest);

    if (response.Status == 202) {

      AppLogger.LogOperationInProgress();

      string location = response.GetLocationHeader();
      int? retryAfter = 5; // response.GetRetryAfterHeader();     
      Guid JobInstanceId = new Guid(location.Substring(location.LastIndexOf("/") + 1));

      Thread.Sleep(retryAfter.Value * 1000);

      var jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Notebook.Id.Value, JobInstanceId).Value;


      while (jobInstance.Status == Status.NotStarted || jobInstance.Status == Status.InProgress) {
        AppLogger.LogOperationInProgress();
        Thread.Sleep(retryAfter.Value * 1000);
        jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Notebook.Id.Value, JobInstanceId).Value;
      }

      AppLogger.LogOperationComplete();

      if (jobInstance.Status == Status.Completed) {
        AppLogger.LogSubstep("Notebook execution completed");
      }

      if (jobInstance.Status == Status.Failed) {
        AppLogger.LogSubstep("Notebook execution failed");
        AppLogger.LogSubstep(jobInstance.FailureReason.Message);
      }

      if (jobInstance.Status == Status.Cancelled) {
        AppLogger.LogSubstep("Notebook execution cancelled");
      }

      if (jobInstance.Status == Status.Deduped) {
        AppLogger.LogSubstep("Notebook execution deduped");
      }
    }
    else {
      AppLogger.LogStep("Notebook execution failed when starting");
    }

  }


  //public static void RunNotebookWithParameters(Guid WorkspaceId, Item Item, Dictionary<string, string> Parameters) {

  //  AppLogger.LogOperationStart($"Running notebook named {Item.DisplayName}");

  //  var paramList = new List<KeyValuePair<string, object>>();

  //  foreach (KeyValuePair<string, string> Parameter in Parameters) {
  //    paramList.Add(new KeyValuePair<string, object>(Parameter.Key, new List<KeyValuePair<string, object>>(){
  //      new KeyValuePair<string, object>("value", Parameter.Value),
  //      new KeyValuePair<string, object>("type", "string")
  //    }));
  //  }

  //  var executionData = new List<KeyValuePair<string, object>>() {
  //    new KeyValuePair<string, object>("parameters", paramList)
  //  };


  //  var jobRequest = new RunOnDemandItemJobRequest {
  //    ExecutionData = executionData
  //  };


  //  var response = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, Item.Id.Value, "RunNotebook", jobRequest);

  //  if (response.Status == 202) {
  //    AppLogger.LogOperationInProgress();

  //    string location = response.GetLocationHeader();
  //    int? retryAfter = 5; // response.GetRetryAfterHeader();     
  //    Guid JobInstanceId = new Guid(location.Substring(location.LastIndexOf("/") + 1));

  //    Thread.Sleep(retryAfter.Value * 1000);

  //    var jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Item.Id.Value, JobInstanceId).Value;


  //    while (jobInstance.Status == Status.NotStarted || jobInstance.Status == Status.InProgress) {
  //      AppLogger.LogOperationInProgress();
  //      Thread.Sleep(retryAfter.Value * 1000);
  //      jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Item.Id.Value, JobInstanceId).Value;
  //    }

  //    AppLogger.LogOperationComplete();

  //    if (jobInstance.Status == Status.Completed) {
  //      AppLogger.LogSubstep("Notebook execution completed");
  //    }

  //    if (jobInstance.Status == Status.Failed) {
  //      AppLogger.LogSubstep("Notebook execution failed");
  //      AppLogger.LogSubstep(jobInstance.FailureReason.Message);
  //    }

  //    if (jobInstance.Status == Status.Cancelled) {
  //      AppLogger.LogSubstep("Notebook execution cancelled");
  //    }

  //    if (jobInstance.Status == Status.Deduped) {
  //      AppLogger.LogSubstep("Notebook execution deduped");
  //    }
  //  }
  //  else {
  //    AppLogger.LogStep("Notebook execution failed when starting");
  //  }

  //}

  //public static void RunNotebookWorkaround(Guid WorkspaceId, Item Item) {

  //  Console.Write(" - Running notebook named " + Item.DisplayName);

  //  var startJobResponse = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, Item.Id.Value, "RunNotebook");


  //  if (startJobResponse.Status == 202) {

  //    string location;
  //    startJobResponse.Headers.TryGetValue("Location", out location);

  //    //Guid JobInstanceId = new Guid(location.Substring(location.LastIndexOf("/") + 1));

  //    string RetryAfterHeader;
  //    startJobResponse.Headers.TryGetValue("Retry-After", out RetryAfterHeader);
  //    int RetryAfter = 15000; //  int.Parse(RetryAfterHeader);

  //    Thread.Sleep(RetryAfter);

  //    var jobInstance = GetJobInstance(location);


  //    while (jobInstance.status == Status.NotStarted || jobInstance.status == Status.InProgress) {
  //      Console.Write(".");
  //      Thread.Sleep(RetryAfter);
  //      jobInstance = GetJobInstance(location);
  //    }

  //    if (jobInstance.status == Status.Completed) {
  //      Console.WriteLine("   > Notebook execution completed");
  //      Console.WriteLine();
  //    }

  //    if (jobInstance.status == Status.Failed) {
  //      Console.WriteLine("   > Notebook execution failed");
  //      //Console.WriteLine("   > " + jobInstance.);
  //      Console.WriteLine();
  //    }

  //    if (jobInstance.status == Status.Cancelled) {
  //      Console.WriteLine("   > Notebook execution cancelled");
  //      Console.WriteLine();
  //    }

  //    if (jobInstance.status == Status.Deduped) {
  //      Console.WriteLine("   > Notebook execution Deduped");
  //      Console.WriteLine();
  //    }
  //  }
  //  else {
  //    Console.WriteLine("   > Notebook execution failed when starting");
  //    Console.WriteLine();

  //  }

  //}

  #region "Workaround for GetJobInstance Bug"

  private static FabricOperation GetJobInstance(string restUri) {

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
    client.DefaultRequestHeaders.Add("Accept", "application/json");

    HttpResponseMessage response = client.GetAsync(restUri).Result;

    if (response.IsSuccessStatusCode) {
      string responseBody = response.Content.ReadAsStringAsync().Result;
      return JsonSerializer.Deserialize<FabricOperation>(responseBody);
    }
    else {
      throw new ApplicationException("ERROR executing HTTP GET request " + response.StatusCode);
    }
  }

  public class FabricOperation {
    public string status { get; set; }
    public DateTime createdTimeUtc { get; set; }
    public DateTime lastUpdatedTimeUtc { get; set; }
    public object percentComplete { get; set; }
    public FabricErrorResponse error { get; set; }
  }

  public class FabricErrorResponse {
    public string errorCode { get; set; }
    public string message { get; set; }
    public string requestId { get; set; }
    public object moreDetails { get; set; }
    public object relatedResource { get; set; }
  }

  public static string GetWarehouseConnection(Guid WorkspaceId, Guid WarehouseId) {

    string endpoint = "/workspaces/" + WorkspaceId.ToString() + "/warehouses/" + WarehouseId.ToString();

    string restUri = AppSettings.FabricUserApiBaseUrl + endpoint;

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
    client.DefaultRequestHeaders.Add("Accept", "application/json");

    HttpResponseMessage response = client.GetAsync(restUri).Result;

    if (response.IsSuccessStatusCode) {
      string jsonResponse = response.Content.ReadAsStringAsync().Result;
      FabricWarehouse warehouse = JsonSerializer.Deserialize<FabricWarehouse>(jsonResponse);
      return warehouse.properties.connectionInfo;
    }
    else {
      throw new ApplicationException("ERROR executing HTTP GET request " + response.StatusCode);
    }

  }

  public class FabricWarehouse {
    public string id { get; set; }
    public string type { get; set; }
    public string displayName { get; set; }
    public string description { get; set; }
    public string workspaceId { get; set; }
    public FabricWarehouseProperties properties { get; set; }
  }

  public class FabricWarehouseProperties {
    public string connectionInfo { get; set; }
    public DateTime createdDate { get; set; }
    public DateTime lastUpdatedTime { get; set; }
  }

  #endregion
}
