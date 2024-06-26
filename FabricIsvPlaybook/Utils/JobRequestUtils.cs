using Microsoft.Fabric.Api.Core.Models;

public class JobRequestUtils {

  public static RunOnDemandItemJobRequest GetJobRequestWithParameters(Dictionary<string, string> Parameters) {

    var paramList = new List<KeyValuePair<string, object>>();

    foreach (KeyValuePair<string, string> Parameter in Parameters) {
      paramList.Add(new KeyValuePair<string, object>(Parameter.Key, new List<KeyValuePair<string, object>>(){
          new KeyValuePair<string, object>("value", Parameter.Value),
          new KeyValuePair<string, object>("type", "string")
        }));
    }

    return new RunOnDemandItemJobRequest {
      ExecutionData = new List<KeyValuePair<string, object>>() {
          new KeyValuePair<string, object>("parameters", paramList)
        }
    };

  }

  public static RunOnDemandItemJobRequest GetJobRequestForTableMaintanance(string TableName) {

    return new RunOnDemandItemJobRequest {
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

  }

}

