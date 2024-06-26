
using Microsoft.Data.SqlClient;

public class SqlConnectionWriter {

  private string datasetServer;
  private string datasetName;
  private SqlConnection connection = new SqlConnection();

  public SqlConnectionWriter(string DatabaseServer, string DatabaseName) {
    this.datasetServer = DatabaseServer;
    this.datasetName = DatabaseName;
    this.SetConnectionWithAccessToken();
  }

  private void SetConnectionWithAccessToken() {
    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
    builder.DataSource = this.datasetServer;
    builder.InitialCatalog = this.datasetName;
    builder.ConnectTimeout = 120;
    this.connection.ConnectionString = builder.ConnectionString;
    this.connection.AccessToken = EntraIdTokenManager.GetAccessTokenForSqlEndPoint();
    this.connection.Open();
  }

  public void ExecuteSql(string sql) {
    SqlCommand sql_command = new SqlCommand(sql, connection);
    sql_command.CommandTimeout = 0;
    sql_command.ExecuteNonQuery();
  }

  public void ExecuteSqlBatch(string sqlBatch) {

    Console.Write(" - Executing SQL Batch");

    string[] sqlStatements = sqlBatch.Split("GO");

    foreach (string sqlStatement in sqlStatements) {
      string sql = sqlStatement.Trim();
      Console.Write(".");
      SqlCommand sql_command = new SqlCommand(sql, connection);
      sql_command.CommandTimeout = 0;
      sql_command.ExecuteNonQuery();
    }

    Console.WriteLine();

  }

}