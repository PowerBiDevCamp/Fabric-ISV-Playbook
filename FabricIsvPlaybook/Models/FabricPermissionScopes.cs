
public class FabricPermissionScopes {

  public const string resourceUri = "https://api.fabric.microsoft.com/";

  // delegated permission scopes used for user token acquisition with custom application
  public static readonly string[] TenantProvisioning = new string[] {
    "https://api.fabric.microsoft.com/Capacity.ReadWrite.All",
    "https://api.fabric.microsoft.com/Workspace.ReadWrite.All",
    "https://api.fabric.microsoft.com/Connection.ReadWrite.All",
    "https://api.fabric.microsoft.com/Item.ReadWrite.All",
    "https://api.fabric.microsoft.com/Item.Read.All",
    "https://api.fabric.microsoft.com/Item.Execute.All",
    "https://api.fabric.microsoft.com/Content.Create",
    "https://api.fabric.microsoft.com/OneLake.ReadWrite.All",
    "https://api.fabric.microsoft.com/Dataset.ReadWrite.All ",
    "https://api.fabric.microsoft.com/Report.ReadWrite.All",
    "https://api.fabric.microsoft.com/Workspace.GitCommit.All",
    "https://api.fabric.microsoft.com/Workspace.GitUpdate.All"
    };

  // delegated permission scope used when authenticating with Azure PowerShell application
  public static readonly string[] User_Impersonation = new string[] {
      "https://api.fabric.microsoft.com/user_impersonation"
  };

  // used for service principal token acquisition
  public static readonly string[] Default = new string[] {
      "https://api.fabric.microsoft.com/.default"
    };
}
