using FishMMO.Database;
using Microsoft.Data.SqlClient;

namespace FishMMO.Installer
{
	/// <summary>
	/// Handles SQL Server provisioning and FishMMO database setup.
	/// </summary>
	public static class SqlServerInstaller
	{
		private static string BuildMasterConnectionString(SqlServerSettings settings)
		{
			var builder = new SqlConnectionStringBuilder
			{
				DataSource = settings.Server,
				InitialCatalog = "master",
				UserID = settings.Username,
				Password = settings.Password,
				TrustServerCertificate = settings.TrustServerCertificate,
				Encrypt = true
			};

			return builder.ConnectionString;
		}

		public static async Task InstallSqlServer(AppSettings appSettings)
		{
			InstallerProcessHelper.Log("SQL Server installation is environment-specific and not yet fully automated.");
			InstallerProcessHelper.Log("Ensure SQL Server is installed and reachable, then continue with FishMMO DB setup.");
			await ValidateConnection(appSettings.SqlServer);
		}

		public static async Task InstallFishMMODatabase(AppSettings appSettings)
		{
			await ValidateConnection(appSettings.SqlServer);

			using var connection = new SqlConnection(BuildMasterConnectionString(appSettings.SqlServer));
			await connection.OpenAsync();

			string dbName = appSettings.SqlServer.Database.Replace("]", "]]", StringComparison.Ordinal);
			string createDbSql = $@"
IF DB_ID(N'{dbName}') IS NULL
BEGIN
    CREATE DATABASE [{dbName}]
END";

			using var command = new SqlCommand(createDbSql, connection);
			await command.ExecuteNonQueryAsync();
			InstallerProcessHelper.Log($"SQL Server database '{appSettings.SqlServer.Database}' is ready.");
		}

		private static async Task ValidateConnection(SqlServerSettings settings)
		{
			try
			{
				using var connection = new SqlConnection(BuildMasterConnectionString(settings));
				await connection.OpenAsync();
				InstallerProcessHelper.Log("SQL Server connectivity check passed.");
			}
			catch (Exception ex)
			{
				InstallerProcessHelper.Log($"Unable to connect to SQL Server: {ex.Message}");
				throw;
			}
		}
	}
}
