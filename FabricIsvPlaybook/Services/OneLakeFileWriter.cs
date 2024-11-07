using Azure.Core;
using Azure.Storage.Files.DataLake;
using Microsoft.Identity.Client;

public class OneLakeTokenCredentials : TokenCredential {

  private static string[] scopes;

  private static AuthenticationResult accessTokenResult;

  private readonly AccessToken accessToken;

  public OneLakeTokenCredentials() {

    if (AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth) {
      scopes = new string[] { "https://storage.azure.com/.default" };
    }
    else {
      scopes = new string[] { "https://storage.azure.com/user_impersonation" };
    }

    accessTokenResult = EntraIdTokenManager.GetAccessTokenResult(scopes);

    accessToken = new AccessToken(accessTokenResult.AccessToken, accessTokenResult.ExpiresOn);
  }

  public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => accessToken;

  public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
      new ValueTask<AccessToken>(Task.FromResult(accessToken));
}

public class OneLakeFileWriter {

  private const string oneLakeUrl = AppSettings.OneLakeBaseUrl;
  private static readonly Uri oneLakeUri = new Uri(oneLakeUrl);

  private string workspaceId;
  private string lakehouseId;

  private DataLakeServiceClient dataLakeServiceClient;
  private DataLakeFileSystemClient fileSystemClient;
  private DataLakeDirectoryClient filesFolder;

  public OneLakeFileWriter(Guid WorkspaceId, Guid LakehouseId) {
    this.workspaceId = WorkspaceId.ToString();
    this.lakehouseId = LakehouseId.ToString();
    this.dataLakeServiceClient = new DataLakeServiceClient(oneLakeUri, new OneLakeTokenCredentials());
    this.fileSystemClient = dataLakeServiceClient.GetFileSystemClient(workspaceId);
    filesFolder = this.fileSystemClient.GetDirectoryClient(lakehouseId + @"\Files");
  }

  public DataLakeDirectoryClient CreateTopLevelFolder(string FolderName) {
    var folder = filesFolder.GetSubDirectoryClient(FolderName);
    folder.CreateIfNotExists();
    return folder;
  }

  public DataLakeFileClient CreateFile(DataLakeDirectoryClient Folder, string FileName, Stream FileContent) {
    var file = Folder.GetFileClient(FileName);
    file.Create();
    file.Upload(FileContent, overwrite: true);
    return file;
  }

}
