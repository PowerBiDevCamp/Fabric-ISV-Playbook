

using System.Net.Http.Headers;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;

#region "Serialization classes"

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

public class FabricConnectionListResponse {
  public List<FabricConnection> value { get; set; }
  public string continuationToken { get; set; }
  public string continuationUri { get; set; }
}

public class FabricConnection {
  public string id { get; set; }
  public string displayName { get; set; }
  public string gatewayId { get; set; }
  public string connectivityType { get; set; }
  public FabricConnectionDetails connectionDetails { get; set; }
  public string privacyLevel { get; set; }
  public FabricCredentialDetails credentialDetails { get; set; }
}

public class FabricConnectivityType {
  public const string ShareableCloud = "ShareableCloud";
  public const string PersonalCloud = "PersonalCloud";
  public const string VirtualNetworkDataGateway = "VirtualNetworkDataGateway";
  public const string OnPremisesDataGateway = "OnPremisesDataGateway";
  public const string OnPremisesDataGatewayPersonal = "OnPremisesDataGatewayPersonal";
}

public class FabricConnectionEncryption {
  public const string Any = "Any";
  public const string Encrypted = "Encrypted";
  public const string NotEncrypted = "NotEncrypted";
}

public class FabricConnectionPrivacyLevel {
  public const string None = "None";
  public const string Organizational = "Organizational";
  public const string Public = "Public";
  public const string Private = "Private";
}

public class FabricCredentialType {
  public const string Anonymous = "Anonymous";
  public const string Basic = "Basic";
  public const string Key = "Key";
  public const string OAuth2 = "OAuth2";
  public const string ServicePrincipal = "ServicePrincipal";
  public const string SharedAccessSignature = "SharedAccessSignature";
  public const string Windows = "Windows";
  public const string WindowsWithoutImpersonation = "WindowsWithoutImpersonation";
  public const string WorkspaceIdentity = "WorkspaceIdentity";
}

public class FabricSingleSignOnType {
  public const string None = "None";
  public const string MicrosoftEntraID = "MicrosoftEntraID";
  public const string Kerberos = "Kerberos";
  public const string KerberosDirectQueryAndRefresh = "KerberosDirectQueryAndRefresh";
  public const string SecurityAssertionMarkupLanguage = "SecurityAssertionMarkupLanguage";
}

public class FabricConnectionDetails {
  public string path { get; set; }
  public string type { get; set; }
}

public class FabricConnectionType {
  public const string SQL = "SQL";
  public const string AzureDataLakeStorage = "AzureDataLakeStorage";
  public const string Web = "Web";
  public const string sharepointlist = "sharepointlist";
}

public class FabricCredentialDetails {
  public string credentialType { get; set; }
  public string singleSignOnType { get; set; }
  public string connectionEncryption { get; set; }
  public bool skipTestConnection { get; set; }
}


public class CreateCloudConnectionRequest {
  public string displayName { get; set; }
  public string connectivityType { get; set; }
  public string privacyLevel { get; set; }
  public CreateConnectionDetails connectionDetails { get; set; }
  public CreateCredentialDetails credentialDetails { get; set; }
}

public class CreateConnectionDetails {
  public string creationMethod { get; set; }
  public string type { get; set; }
  public List<ConnectionParameter> parameters { get; set; }

}

public class ConnectionParameter {
  public string name { get; set; }
  public string dataType { get; set; }
  public string value { get; set; }
}

public class CreateCredentialDetails {
  public string singleSignOnType { get; set; }
  public string connectionEncryption { get; set; }
  public bool skipTestConnection { get; set; }
  public object credentials { get; set; }
}

public class CreateConnectionCredential {
  public string credentialType { get; set; }
}

public class AnonymousCredentials : CreateConnectionCredential { }

public class BasicCrednetial : CreateConnectionCredential {
  public string username { get; set; }
  public string password { get; set; }
}

public class KeyCredential : CreateConnectionCredential {
  public string key { get; set; }

}

public class ServicePrincipalCredentials : CreateConnectionCredential {
  public string tenantId { get; set; }
  public string servicePrincipalClientId { get; set; }
  public string servicePrincipalSecret { get; set; }
}

public class SharedAccessSignatureCredentials : CreateConnectionCredential {
  public string token { get; set; }

}

public class WindowsCredentials : CreateConnectionCredential {
  public string username { get; set; }
  public string password { get; set; }
}

public class WindowsWithoutImpersonationCredentials : CreateConnectionCredential { }

public class WorkspaceIdentityCredentials : CreateConnectionCredential { }

#endregion

public class FabricConnectionsApi {

  #region "low-level plumbing details"

  private static string AccessToken = EntraIdTokenManager.GetFabricAccessToken();

  private static string ExecuteGetRequest(string endpoint) {

    string restUri = AppSettings.FabricUserApiBaseUrl + endpoint;

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
    client.DefaultRequestHeaders.Add("Accept", "application/json");

    HttpResponseMessage response = client.GetAsync(restUri).Result;

    if (response.IsSuccessStatusCode) {
      return response.Content.ReadAsStringAsync().Result;
    }
    else {
      throw new ApplicationException("ERROR executing HTTP GET request " + response.StatusCode);
    }
  }

  private static string ExecutePostRequest(string endpoint, string postBody = "") {

    string restUri = AppSettings.FabricUserApiBaseUrl + endpoint;

    HttpContent body = new StringContent(postBody);
    body.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);

    HttpResponseMessage response = client.PostAsync(restUri, body).Result;

    // switch to handle responses with different status codes
    switch (response.StatusCode) {

      // handle case when sync call succeeds with OK (200) or CREATED (201)
      case HttpStatusCode.Created:
        // return result to caller
        return response.Content.ReadAsStringAsync().Result;

      default: // handle exeception where HTTP status code indicates failure
        throw new ApplicationException("ERROR executing HTTP POST request " + response.StatusCode);
    }

  }

  private static string ExecutePatchRequest(string endpoint, string postBody = "") {

    string restUri = AppSettings.FabricUserApiBaseUrl + endpoint;

    HttpContent body = new StringContent(postBody);
    body.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);

    HttpResponseMessage response = client.PatchAsync(restUri, body).Result;

    if (response.IsSuccessStatusCode) {
      return response.Content.ReadAsStringAsync().Result;
    }
    else {
      throw new ApplicationException("ERROR executing HTTP PATCH request " + response.StatusCode);
    }
  }

  private static string ExecuteDeleteRequest(string endpoint) {
    string restUri = AppSettings.FabricUserApiBaseUrl + endpoint;

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
    HttpResponseMessage response = client.DeleteAsync(restUri).Result;

    if (response.IsSuccessStatusCode) {
      return response.Content.ReadAsStringAsync().Result;
    }
    else {
      throw new ApplicationException("ERROR executing HTTP DELETE request " + response.StatusCode);
    }
  }

  private static JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  #endregion

  public static List<FabricConnection> GetConnections() {
    string jsonResponse = ExecuteGetRequest("/connections");
    return JsonSerializer.Deserialize<FabricConnectionListResponse>(jsonResponse).value;
  }

  public static FabricConnection GetConnection(string ConnectionId) {
    string jsonResponse = ExecuteGetRequest($"/connections/{ConnectionId}");
    return JsonSerializer.Deserialize<FabricConnection>(jsonResponse);
  }

  public static void DisplayConnnections() {
    var connections = GetConnections();

    foreach (var connection in connections) {
      Console.WriteLine($"Connection: {connection.id}");
      Console.WriteLine($" - Display Name: {connection.displayName}");
      Console.WriteLine($" - Connectivity Type: {connection.connectivityType}");
      Console.WriteLine($" - Connection type: {connection.connectionDetails.type}");
      Console.WriteLine($" - Connection path: {connection.connectionDetails.path}");
      Console.WriteLine();
    }
  }

  public static void DeleteConnection(string ConnectionId) {

    try {
      ExecuteDeleteRequest("/connections/" + ConnectionId);
    }
    catch {
      // do nothing - this is logic to handle bug with delete connection returning error
    }
  }

  public static void DeleteAllConnections() {
    AppLogger.LogOperationStart("Deleting all connections");
    foreach (var connection in GetConnections()) {
      AppLogger.LogOperationInProgress();
      DeleteConnection(connection.id);
      Thread.Sleep(6000);
    }
    AppLogger.LogOperationComplete();
  }

  public static void DeleteAllPersonalCloudConnections() {
    AppLogger.LogOperationStart("Deleting personal cloud connections");
    foreach (var connection in GetConnections()) {
      if(connection.connectivityType == FabricConnectivityType.PersonalCloud) {
        AppLogger.LogOperationInProgress();
        DeleteConnection(connection.id);
        Thread.Sleep(6000);
      }
    }
    AppLogger.LogOperationComplete();
  }

  public static void DeleteConnectionIfItExists(string ConnectionName) {

    var connections = GetConnections();

    foreach (var connection in connections) {
      if (connection.displayName == ConnectionName) {
        Console.WriteLine("Deleting existing connection");
        ExecuteDeleteRequest("/connections/" + connection.id);
      }
    }

  }

  public static FabricConnection CreateConnection(CreateCloudConnectionRequest CreateConnectionRequest, bool UseExisting = true) {

    var connections = GetConnections();

    foreach (var existingConnection in connections) {
      if (!string.IsNullOrEmpty(existingConnection.displayName) && existingConnection.displayName == CreateConnectionRequest.displayName) {
        if (UseExisting) {
          return existingConnection;
        }
        else {
          DeleteConnection(existingConnection.id);
        }
        
      }
    }

    string requestBody = JsonSerializer.Serialize(CreateConnectionRequest, jsonOptions);

    string jsonResponse = ExecutePostRequest("/connections", requestBody);

    FabricConnection connection = JsonSerializer.Deserialize<FabricConnection>(jsonResponse);

    return connection;
  }

  public static FabricConnection CreateAnonymousWebConnection(string Url) {

    string creator = AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth ? "SP1-" : "User1-";
    string connectionName = creator + "Web-Anonymous- " + Url;

    var createConnectionRequest = new CreateCloudConnectionRequest {
      displayName = connectionName,
      connectivityType = FabricConnectivityType.ShareableCloud,
      privacyLevel = FabricConnectionPrivacyLevel.Organizational,
      connectionDetails = new CreateConnectionDetails {
        creationMethod = FabricConnectionType.Web,
        type = FabricConnectionType.Web,
        parameters = new List<ConnectionParameter> {
         new ConnectionParameter {
           name = "url", value=Url, dataType = "text"
         }
       },

      },
      credentialDetails = new CreateCredentialDetails {
        singleSignOnType = FabricSingleSignOnType.None,
        connectionEncryption = FabricConnectionEncryption.NotEncrypted,
        skipTestConnection = false,
        credentials = new AnonymousCredentials {
          credentialType = FabricCredentialType.Anonymous
        }
      }
    };

    AppLogger.LogStep("Creating Web connection based on Anonymous Access Auth");
    var connection = CreateConnection(createConnectionRequest);
    AppLogger.LogSubstep($"Connection created with Id of {connection.id}");

    return connection;
  }

  public static FabricConnection CreateAzureStorageConnection(string Server, string Path) {

    string creator = AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth ? "SP1-" : "User1-";
    string connectionName = $"{creator}-ADLS-AccountKey-{Server}-{Path}";

    var createConnectionRequest = new CreateCloudConnectionRequest {
      displayName = connectionName,
      connectivityType = FabricConnectivityType.ShareableCloud,
      privacyLevel = FabricConnectionPrivacyLevel.None,
      connectionDetails = new CreateConnectionDetails {
        creationMethod = FabricConnectionType.AzureDataLakeStorage,
        type = FabricConnectionType.AzureDataLakeStorage,
        parameters = new List<ConnectionParameter> {
         new ConnectionParameter {
           name = "server", value=Server, dataType = "text"
         },
         new ConnectionParameter {
           name = "path", value=Path, dataType = "text"
         }
       }
      },
      credentialDetails = new CreateCredentialDetails {
        singleSignOnType = FabricSingleSignOnType.None,
        connectionEncryption = FabricConnectionEncryption.NotEncrypted,
        skipTestConnection = false,
        credentials = new KeyCredential {
          key = AppSettings.AzureStorageAccountKey,
          credentialType = FabricCredentialType.Key
        }
      }
    };

    AppLogger.LogStep("Creating connection to Azure Storage using account key credentials");
    var connection = CreateConnection(createConnectionRequest);
    AppLogger.LogSubstep($"Connection created with Id of {connection.id}");

    return connection;
  }

  public static FabricConnection CreateAzureStorageConnectionWithSPN(string Server, string Path) {

    string creator = AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth ? "SP1-" : "User1-";
    string connectionName = $"{creator}-ADLS-ServicePrincipal-{Server}-{Path}";

    var createConnectionRequest = new CreateCloudConnectionRequest {
      displayName = connectionName,
      connectivityType = FabricConnectivityType.ShareableCloud,
      privacyLevel = FabricConnectionPrivacyLevel.Organizational,
      connectionDetails = new CreateConnectionDetails {
        creationMethod = FabricConnectionType.AzureDataLakeStorage,
        type = FabricConnectionType.AzureDataLakeStorage,
        parameters = new List<ConnectionParameter> {
         new ConnectionParameter {
           name = "server", value=Server, dataType = "text"
         },
         new ConnectionParameter {
           name = "path", value=Path, dataType = "text"
         }
       }
      },
      credentialDetails = new CreateCredentialDetails {
        singleSignOnType = FabricSingleSignOnType.None,
        connectionEncryption = FabricConnectionEncryption.NotEncrypted,
        skipTestConnection = false,
        credentials = new ServicePrincipalCredentials {
          credentialType = FabricCredentialType.ServicePrincipal,
          tenantId = AppSettings.ServicePrincipalAuthTenantId,
          servicePrincipalClientId = AppSettings.ServicePrincipalAuthClientId,
          servicePrincipalSecret = AppSettings.ServicePrincipalAuthClientSecret
        }
      }
    };

    AppLogger.LogStep("Creating connection to Azure Storage using service principal credentials");
    var connection = CreateConnection(createConnectionRequest);
    AppLogger.LogSubstep($"Connection created with Id of {connection.id}");

    return connection;

  }

  public static FabricConnection CreateSqlEndpointConnectionWithWorkspaceIdentity(string Server, string Database) {

    string creator = AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth ? "SP1-" : "User1-";
    string connectionName = $"{creator}-xxxx-BasicAuth-{Server}-{Database}";

    var createConnectionRequest = new CreateCloudConnectionRequest {
      displayName = connectionName,
      connectivityType = FabricConnectivityType.ShareableCloud,
      privacyLevel = FabricConnectionPrivacyLevel.Organizational,
      connectionDetails = new CreateConnectionDetails {
        creationMethod = FabricConnectionType.SQL,
        type = FabricConnectionType.SQL,
        parameters = new List<ConnectionParameter> {
         new ConnectionParameter {
           name = "server", value=Server, dataType = "text"
         },
         new ConnectionParameter {
           name = "database", value=Database, dataType = "text"
         }
       }
      },
      credentialDetails = new CreateCredentialDetails {
        singleSignOnType = FabricSingleSignOnType.None,
        connectionEncryption = FabricConnectionEncryption.NotEncrypted,
        skipTestConnection = false,
        credentials = new WorkspaceIdentityCredentials {
          credentialType = FabricCredentialType.WorkspaceIdentity
        }
      }
    };

    AppLogger.LogStep("Creating SQL endpont connection to Workspace Identity");
    var connection = CreateConnection(createConnectionRequest);
    AppLogger.LogSubstep($"Connection created with Id of {connection.id}");

    return connection;

  }

  public static FabricConnection CreateSqlConnection(string Server, string Database) {

    string creator = AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth ? "SP1-" : "User1-";
    string connectionName = $"{creator}-SQL-BasicAuth-{Server}:{Database}";

    var createConnectionRequest = new CreateCloudConnectionRequest {
      displayName = connectionName,
      connectivityType = FabricConnectivityType.ShareableCloud,
      privacyLevel = FabricConnectionPrivacyLevel.Organizational,
      connectionDetails = new CreateConnectionDetails {
        creationMethod = "SQL",
        type = FabricConnectionType.SQL,
        parameters = new List<ConnectionParameter> {
         new ConnectionParameter {
           name = "server", value=Server, dataType = "text"
         },
         new ConnectionParameter {
           name = "database", value=Database, dataType = "text"
         }

       }
      },
      credentialDetails = new CreateCredentialDetails {
        singleSignOnType = FabricSingleSignOnType.None,
        connectionEncryption = FabricConnectionEncryption.NotEncrypted,
        skipTestConnection = false,
        credentials = new BasicCrednetial {
          credentialType = FabricCredentialType.Basic,
          username = AppSettings.SqlUser,
          password = AppSettings.SqlUserPassword

        }
      }
    };

    AppLogger.LogStep("Creating SQL connection to Azure SQL using Basic authentication");
    var connection = CreateConnection(createConnectionRequest);
    AppLogger.LogSubstep($"Connection created with Id of {connection.id}");

    return connection;

  }

  public static FabricConnection CreateSqlEndpointConnectionUsingServicePrincipal(string Server, string Database) {

    string creator = AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth ? "SP1-" : "User1-";
    string connectionName = $"{creator}-SQL-ServicePrincipal-{Server}:{Database}";
    
    var createConnectionRequest = new CreateCloudConnectionRequest {
      displayName = connectionName,
      connectivityType = FabricConnectivityType.ShareableCloud,
      privacyLevel = FabricConnectionPrivacyLevel.Organizational,
      connectionDetails = new CreateConnectionDetails {
        creationMethod = "SQL",
        type = FabricConnectionType.SQL,
        parameters = new List<ConnectionParameter> {
         new ConnectionParameter {
           name = "server", value=Server, dataType = "text"
         },
         new ConnectionParameter {
           name = "database", value=Database, dataType = "text"
         }

       }
      },
      credentialDetails = new CreateCredentialDetails {
        singleSignOnType = FabricSingleSignOnType.None,
        connectionEncryption = FabricConnectionEncryption.NotEncrypted,
        skipTestConnection = false,
        credentials = new ServicePrincipalCredentials {
          credentialType = FabricCredentialType.ServicePrincipal,
          tenantId = AppSettings.ServicePrincipalAuthTenantId,
          servicePrincipalClientId = AppSettings.ServicePrincipalAuthClientId,
          servicePrincipalSecret = AppSettings.ServicePrincipalAuthClientSecret
        }
      }
    };

    AppLogger.LogStep("Creating connection to SQL endpoint using service principal credentials");
    var connection = CreateConnection(createConnectionRequest);
    AppLogger.LogSubstep($"Connection created with Id of {connection.id}");

    return connection;

  }

}
