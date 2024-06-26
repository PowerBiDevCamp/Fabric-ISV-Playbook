public class FabricPermissionScopes {

  public const string resourceUri = "https://api.fabric.microsoft.com/";

  // delegated permission scopes used for user token acquisition 
  public static readonly string[] Fabric_User_Impresonation = new string[] {
      "https://api.fabric.microsoft.com/user_impersonation"
  };

  // delegated permission scopes used for user token acquisition with custom application
  public static readonly string[] TenantProvisioning = new string[] {
      "https://api.fabric.microsoft.com/Capacity.ReadWrite.All",
      "https://api.fabric.microsoft.com/Workspace.ReadWrite.All",
      "https://api.fabric.microsoft.com/Item.ReadWrite.All",
      "https://api.fabric.microsoft.com/Item.Read.All",
      "https://api.fabric.microsoft.com/Item.Execute.All",
      "https://api.fabric.microsoft.com/Content.Create",
      "https://api.fabric.microsoft.com/Dataset.ReadWrite.All ",
      "https://api.fabric.microsoft.com/Report.ReadWrite.All",
      "https://api.fabric.microsoft.com/Workspace.GitCommit.All",
    };

  // this default permission will be used when service principal support is added
  public static readonly string[] Default = new string[] {
      "https://api.fabric.microsoft.com/.default"
    };

}
