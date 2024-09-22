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
using System.Text;

public class FabricRestApi {

  private static string accessToken;
  private static FabricClient fabricApiClient;

  static FabricRestApi() {
    accessToken = EntraIdTokenManager.GetFabricAccessToken();
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

    if(AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth){
      Guid AdminUserId = new Guid(AppSettings.AdminUser1Id);
      FabricRestApi.AddUserAsWorkspaceMemeber(workspace.Id, AdminUserId, WorkspaceRole.Admin);
    }
    else {
      Guid ServicePrincipalObjectId = new Guid(AppSettings.ServicePrincipalObjectId);
      FabricRestApi.AddServicePrincipalAsWorkspaceMemeber(workspace.Id, ServicePrincipalObjectId, WorkspaceRole.Admin);
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

  public static void ProvisionWorkspaceIdentity(Guid WorkspaceId) {
    AppLogger.LogOperationStart("Provisioning workspace identity...");
    fabricApiClient.Core.Workspaces.ProvisionIdentity(WorkspaceId);
    AppLogger.LogOperationComplete();
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

  public static List<Item> GetItems(Guid WorkspaceId, string ItemType = null) {
    return fabricApiClient.Core.Items.ListItems(WorkspaceId, ItemType).ToList();
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

  public static void UpdateItemDefinition(Guid WorkspaceId, Guid ItemId, UpdateItemDefinitionRequest UpdateRequest) {
    fabricApiClient.Core.Items.UpdateItemDefinition(WorkspaceId, ItemId, UpdateRequest);
  }

  public static ItemDefinition UpdateItemDefinitionPart(ItemDefinition ItemDefinition, string PartPath, Dictionary<string, string> SearchReplaceText) {
    var itemPart = ItemDefinition.Parts.Where(part => part.Path == PartPath).First();
    ItemDefinition.Parts.Remove(itemPart);
    itemPart.Payload = SearchAndReplaceInPayload(itemPart.Payload, SearchReplaceText);
    ItemDefinition.Parts.Add(itemPart);
    return ItemDefinition;
  }

  public static string SearchAndReplaceInPayload(string Payload, Dictionary<string, string> SearchReplaceText) {
    byte[] PayloadBytes = Convert.FromBase64String(Payload);
    string PayloadContent = Encoding.UTF8.GetString(PayloadBytes, 0, PayloadBytes.Length);
    foreach (var entry in SearchReplaceText.Keys) {
      PayloadContent = PayloadContent.Replace(entry, SearchReplaceText[entry]);
    }
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(PayloadContent));
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

  public static string GetSqlConnectionStringForWarehouse(Guid WorkspaceId, Guid WarehouseId) {
    var warehouse = GetWarehouse(WorkspaceId, WarehouseId);
    return warehouse.Properties.ConnectionString;
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
      int? retryAfter = 10; // response.GetRetryAfterHeader();     
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

  public static void RunDataPipeline(Guid WorkspaceId, Item DataPipeline) {

    AppLogger.LogOperationStart($"Running DataPipeline named {DataPipeline.DisplayName}");

    var response = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, DataPipeline.Id.Value, "Pipeline");

    if (response.Status == 202) {

      AppLogger.LogOperationInProgress();

      string location = response.GetLocationHeader();
      int? retryAfter = 10; // response.GetRetryAfterHeader();
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

  public static void CreateShortcut(Guid WorkspaceId, Guid LakehouseId, CreateShortcutRequest CreateShortcutRequest) {
    var response = fabricApiClient.Core.OneLakeShortcuts.CreateShortcut(WorkspaceId, LakehouseId, CreateShortcutRequest).Value;    
  }

  public static void CreateAdlsGen2Shortcut(Guid WorkspaceId, Guid LakehouseId, Uri Location, string Path, string Name, Guid ConnectionId) {

    AppLogger.LogStep("Creating OneLake Shortcut to ADLS Gen2 Storage");

    var target = new CreatableShortcutTarget {
      AdlsGen2 = new AdlsGen2(Location, Name, ConnectionId)
    };

    var createRequest = new CreateShortcutRequest(Path,Name, target);
    var response = fabricApiClient.Core.OneLakeShortcuts.CreateShortcut(WorkspaceId, LakehouseId, createRequest).Value;

    AppLogger.LogSubstep($"OneLake shortcut created");
  }

  // support for exporting item definition parts as files on local file system

  public static void ExportItemDefinitionsFromWorkspace(string WorkspaceName) {

    AppLogger.LogStep($"Exporting workspaces items from {WorkspaceName}");

    DeleteAllTemplateFiles(WorkspaceName);

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

          string exportFormat = item.Type.ToString() switch {
            "Report" => "PBIR-Legacy",    // PBIR or PBIR-Legacy
            "SemanticModel" => "TMSL",    // TMSL or TMDL
            "Notebook" => "ipynb",
            _ => null,
          };

          var definition = GetItemDefinition(workspace.Id, item.Id.Value, exportFormat);

          string targetFolder = item.DisplayName + "." + item.Type;

          AppLogger.LogSubstep($"Exporting {targetFolder}");

          foreach (var part in definition.Parts) {
            WriteFile(WorkspaceName, targetFolder, part.Path, part.Payload);
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

  public static void DeleteAllTemplateFiles(string WorkspaceName) {
    string targetFolder = AppSettings.LocalTemplatesFolder + (string.IsNullOrEmpty(WorkspaceName) ? "" : WorkspaceName + @"\");
    if (Directory.Exists(targetFolder)) {
      DirectoryInfo di = new DirectoryInfo(targetFolder);
      foreach (FileInfo file in di.GetFiles()) { file.Delete(); }
      foreach (DirectoryInfo dir in di.GetDirectories()) { dir.Delete(true); }
    }
  }

  public static void WriteFile(string WorkspaceFolder, string ItemFolder, string FilePath, string FileContent, bool ConvertFromBase64 = true) {

    if (ConvertFromBase64) {
      byte[] bytes = Convert.FromBase64String(FileContent);
      FileContent = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    }

    FilePath = FilePath.Replace("/", @"\");
    string folderPath = AppSettings.LocalTemplatesFolder + WorkspaceFolder + @"\" + ItemFolder;

    Directory.CreateDirectory(folderPath);

    string fullPath = folderPath + @"\" + FilePath;

    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

    File.WriteAllText(fullPath, FileContent);

  }

}