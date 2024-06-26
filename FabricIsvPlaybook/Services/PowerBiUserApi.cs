using Microsoft.PowerBI.Api.Models;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;
using System.Net.Http.Headers;

public class PowerBiUserApi {

  private static PowerBIClient pbiClient;

  static PowerBiUserApi() {
    string accessToken = EntraIdTokenManager.GetAccessToken(FabricPermissionScopes.Fabric_User_Impresonation); string urlPowerBiServiceApiRoot = AppSettings.PowerBiRestApiBaseUrl;
    var tokenCredentials = new TokenCredentials(accessToken, "Bearer");
    pbiClient = new PowerBIClient(new Uri(urlPowerBiServiceApiRoot), tokenCredentials);
  }

  public static void RefreshDataset(Guid WorkspaceId, Guid DatasetId) {

    AppLogger.LogOperationStart($"Refreshing semantic model");

    var refreshRequest = new DatasetRefreshRequest {
      NotifyOption = NotifyOption.NoNotification,
      Type = DatasetRefreshType.Automatic
    };

    var responseStartFresh = pbiClient.Datasets.RefreshDatasetInGroup(WorkspaceId, DatasetId.ToString(), refreshRequest);

    AppLogger.LogOperationInProgress();

    var responseStatusCheck = pbiClient.Datasets.GetRefreshExecutionDetailsInGroup(WorkspaceId, DatasetId, new Guid(responseStartFresh.XMsRequestId));

    while (responseStatusCheck.Status == "Unknown") {
      Thread.Sleep(3000);
      AppLogger.LogOperationInProgress();
      responseStatusCheck = pbiClient.Datasets.GetRefreshExecutionDetailsInGroup(WorkspaceId, DatasetId, new Guid(responseStartFresh.XMsRequestId));
    }

    AppLogger.LogOperationComplete();
  }

  public static IList<Datasource> GetDatasourcesForDataset(string WorkspaceId, string DatasetId) {
    return pbiClient.Datasets.GetDatasourcesInGroup(new Guid(WorkspaceId), DatasetId).Value;
  }

  public static void ViewDatasources(Guid WorkspaceId, Guid DatasetId) {

    // get datasources for dataset
    var datasources = pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId.ToString()).Value;

    foreach (var datasource in datasources) {

      Console.WriteLine(" - Connection Name: " + datasource.Name);
      Console.WriteLine("   > DatasourceType: " + datasource.DatasourceType);
      Console.WriteLine("   > DatasourceId: " + datasource.DatasourceId);
      Console.WriteLine("   > GatewayId: " + datasource.GatewayId);
      Console.WriteLine("   > Path: " + datasource.ConnectionDetails.Path);
      Console.WriteLine("   > Server: " + datasource.ConnectionDetails.Server);
      Console.WriteLine("   > Database: " + datasource.ConnectionDetails.Database);
      Console.WriteLine("   > Url: " + datasource.ConnectionDetails.Url);
      Console.WriteLine("   > Domain: " + datasource.ConnectionDetails.Domain);
      Console.WriteLine("   > EmailAddress: " + datasource.ConnectionDetails.EmailAddress);
      Console.WriteLine("   > Kind: " + datasource.ConnectionDetails.Kind);
      Console.WriteLine("   > LoginServer: " + datasource.ConnectionDetails.LoginServer);
      Console.WriteLine("   > ClassInfo: " + datasource.ConnectionDetails.ClassInfo);
      Console.WriteLine();

    }
  }

  public static string GetWebDatasourceUrl(Guid WorkspaceId, Guid DatasetId) {

    // get datasources for dataset
    var datasource = pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId.ToString()).Value.First();
    if (datasource.DatasourceType.Equals("Web")) {
      return datasource.ConnectionDetails.Url;
    }
    else {
      throw new ApplicationException("Error - expecting Web connection");

    }
  }

  public static void PatchAnonymousAccessWebCredentials(Guid WorkspaceId, Guid DatasetId) {

    // get datasources for dataset
    var datasources = pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId.ToString()).Value;

    foreach (var datasource in datasources) {

      // check to ensure datasource use Web connector
      if (datasource.DatasourceType.ToLower() == "web") {

        // get DatasourceId and GatewayId
        var datasourceId = datasource.DatasourceId;
        var gatewayId = datasource.GatewayId;

        // Initialize UpdateDatasourceRequest object with AnonymousCredentials
        UpdateDatasourceRequest req = new UpdateDatasourceRequest {
          CredentialDetails = new CredentialDetails(
            new Microsoft.PowerBI.Api.Models.Credentials.AnonymousCredentials(),
            PrivacyLevel.Organizational,
            EncryptedConnection.NotEncrypted)
        };

        // Update datasource credentials through Gateways - UpdateDatasource
        pbiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);

      }
    }
  }

  public static void PatchDirectLakeDatasetCredentials(Guid WorkspaceId, Guid DatasetId) {

    AppLogger.LogOperationStart("Patching credentials for DirectLake semantic model");

    BindToGatewayRequest bindRequest = new BindToGatewayRequest {
      GatewayObjectId = new Guid("00000000-0000-0000-0000-000000000000")
    };

    pbiClient.Datasets.BindToGatewayInGroup(WorkspaceId, DatasetId.ToString(), bindRequest);

    var datasources = pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId.ToString()).Value;

    // update credentials for all SQL datasources
    foreach (var datasource in datasources) {
      if (datasource.DatasourceType.ToLower() == "sql") {

        var datasourceId = datasource.DatasourceId;
        var gatewayId = datasource.GatewayId;

        // create credential details
        var CredentialDetails = new CredentialDetails();
        CredentialDetails.CredentialType = CredentialType.OAuth2;
        CredentialDetails.UseCallerAADIdentity = true;
        CredentialDetails.EncryptedConnection = EncryptedConnection.Encrypted;
        CredentialDetails.EncryptionAlgorithm = EncryptionAlgorithm.None;
        CredentialDetails.PrivacyLevel = PrivacyLevel.Organizational;

        // create UpdateDatasourceRequest 
        UpdateDatasourceRequest req = new UpdateDatasourceRequest(CredentialDetails);

        // Execute Patch command to update Azure SQL datasource credentials
        pbiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);

      }

      AppLogger.LogOperationComplete();
    }


  }

  public static void RefreshLakehouseSqlEndointSchema(string DatabaseId) {

    var access_token = EntraIdTokenManager.GetAccessToken(FabricPermissionScopes.Fabric_User_Impresonation);

    string restUri = $"https://api.powerbi.com/v1.0/myorg/lhdatamarts/{DatabaseId}";
    string postBody = "{datamartVersion: 5, commands: [{$type: \"MetadataRefreshCommand\"}]}";
    HttpContent body = new StringContent(postBody);
    body.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

    HttpResponseMessage response = client.PostAsync(restUri, body).Result;
  }


  public static void BindDatasetToConnection(string WorkspaceId, string DatasetId, string ConnectionId) {

    BindToGatewayRequest bindRequest = new BindToGatewayRequest {
      // GatewayObjectId = new Guid("00000000-0000-0000-0000-000000000000"),
      DatasourceObjectIds = new List<Guid?>()
    };

    bindRequest.DatasourceObjectIds.Add(new Guid(ConnectionId));

    pbiClient.Datasets.BindToGatewayInGroup(new Guid(WorkspaceId), DatasetId, bindRequest);

  }

  public static void ViewWorkspaces() {
    var workspaces = pbiClient.Groups.GetGroups().Value;
    foreach (var workspace in workspaces) {
      Console.WriteLine(workspace.Name);
    }
  }

}

