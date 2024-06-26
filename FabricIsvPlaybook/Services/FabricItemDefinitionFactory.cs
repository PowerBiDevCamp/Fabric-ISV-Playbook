using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;
using System.Text;

public class FabricItemDefinitionFactory {

  private static ItemDefinitionPart CreateInlineBase64Part(string Path, string Payload) {
    string base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Payload));
    return new ItemDefinitionPart(Path, base64Payload, PayloadType.InlineBase64);
  }

  public static CreateItemRequest GetImportedSalesModelCreateRequest(string DisplayName) {

    string part1FileContent = FabricIsvPlaybook.Properties.Resources.definition_pbism;
    string part2FileContent = FabricIsvPlaybook.Properties.Resources.sales_model_import_bim;

    var createRequest = new CreateItemRequest(DisplayName, ItemType.SemanticModel);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbism", part1FileContent),
        CreateInlineBase64Part("model.bim", part2FileContent)
      });

    return createRequest;
  }

  public static UpdateItemDefinitionRequest GetImportedSalesModelUpdateRequest(string DisplayName) {

    string part1FileContent = FabricIsvPlaybook.Properties.Resources.definition_pbism;
    string part2FileContent = FabricIsvPlaybook.Properties.Resources.sales_model_import_v2_bim;

    return new UpdateItemDefinitionRequest(
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbism", part1FileContent),
        CreateInlineBase64Part("model.bim", part2FileContent)
      }));
  }

  public static CreateItemRequest GetSalesReportCreateRequest(Guid SemanticModelId, string DisplayName) {

    string part1FileContent = FabricIsvPlaybook.Properties.Resources.definition_pbir.Replace("{SEMANTIC_MODEL_ID}", SemanticModelId.ToString());
    string part2FileContent = FabricIsvPlaybook.Properties.Resources.sales_report_json;
    string part3FileContent = FabricIsvPlaybook.Properties.Resources.CY24SU02_json;

    var createRequest = new CreateItemRequest(DisplayName, ItemType.Report);

    createRequest.Definition =
          new ItemDefinition(new List<ItemDefinitionPart>() {
            CreateInlineBase64Part("definition.pbir", part1FileContent),
            CreateInlineBase64Part("report.json", part2FileContent),
            CreateInlineBase64Part("StaticResources/SharedResources/BaseThemes/CY24SU02.json", part3FileContent),
          });

    return createRequest;

  }

  public static UpdateItemDefinitionRequest GetSalesReportUpdateRequest(Guid SemanticModelId, string DisplayName) {

    string part1FileContent = FabricIsvPlaybook.Properties.Resources.definition_pbir.Replace("{SEMANTIC_MODEL_ID}", SemanticModelId.ToString());
    string part2FileContent = FabricIsvPlaybook.Properties.Resources.sales_report_v2_json;
    string part3FileContent = FabricIsvPlaybook.Properties.Resources.CY24SU02_json;
    string part4FileContent = FabricIsvPlaybook.Properties.Resources.NewExecutive_json;

    return new UpdateItemDefinitionRequest(
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbir", part1FileContent),
        CreateInlineBase64Part("report.json", part2FileContent),
        CreateInlineBase64Part("StaticResources/SharedResources/BaseThemes/CY24SU02.json", part3FileContent),
        CreateInlineBase64Part("StaticResources/SharedResources/BuiltInThemes/NewExecutive.json", part4FileContent)
      }));
  }

  public static CreateItemRequest GetNotebookCreateRequest(Guid WorkspaceId, Item Lakehouse, string DisplayName, string CodeContent) {

    CodeContent = CodeContent.Replace("{WORKSPACE_ID}", WorkspaceId.ToString())
                             .Replace("{LAKEHOUSE_ID}", Lakehouse.Id.ToString())
                             .Replace("{LAKEHOUSE_NAME}", Lakehouse.DisplayName);

    var createRequest = new CreateItemRequest(DisplayName, ItemType.Notebook);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("notebook-content.ipynb", CodeContent)
      });

    createRequest.Definition.Format = "ipynb";

    return createRequest;

  }

  public static CreateItemRequest GetDirectLakeSalesModelCreateRequest(string DisplayName, string SqlEndpointServer, string SqlEndpointDatabase) {

    string part1FileContent = FabricIsvPlaybook.Properties.Resources.definition_pbism;

    string part2FileContent = FabricIsvPlaybook.Properties.Resources.sales_model_DirectLake_bim
                                          .Replace("{SQL_ENDPOINT_SERVER}", SqlEndpointServer)
                                          .Replace("{SQL_ENDPOINT_DATABASE}", SqlEndpointDatabase);

    var createRequest = new CreateItemRequest(DisplayName, ItemType.SemanticModel);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbism", part1FileContent),
        CreateInlineBase64Part("model.bim", part2FileContent)
      });

    return createRequest;
  }

  public static CreateItemRequest GetDataPipelineCreateRequest(string DisplayName, string CodeContent) {

    var createRequest = new CreateItemRequest(DisplayName, ItemType.DataPipeline);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("pipeline-content.json", CodeContent)
      });

    return createRequest;
  }

  public static CreateItemRequest GetDataPipelineCreateRequestForLakehouse(string DisplayName, string CodeContent, string WorkspaceId, string LakehouseId, string ConnectionId) {

    var createRequest = new CreateItemRequest(DisplayName, ItemType.DataPipeline);

    CodeContent = CodeContent
      .Replace("{CONNECTION_ID}", ConnectionId)
      .Replace("{WORKSPACE_ID}", WorkspaceId)
      .Replace("{LAKEHOUSE_ID}", LakehouseId);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("pipeline-content.json", CodeContent)
      });

    return createRequest;
  }

  public static CreateItemRequest GetDataPipelineCreateRequestForWarehouse(string DisplayName, string CodeContent, string WorkspaceId, string WarehouseId, string WarehouseConnectString) {

    var createRequest = new CreateItemRequest(DisplayName, ItemType.DataPipeline);

    CodeContent = CodeContent
      .Replace("{WORKSPACE_ID}", WorkspaceId)
      .Replace("{WAREHOUSE_ID}", WarehouseId)
      .Replace("{WAREHOUSE_CONNECT_STRING}", WarehouseConnectString);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("pipeline-content.json", CodeContent)
      });

    return createRequest;
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


