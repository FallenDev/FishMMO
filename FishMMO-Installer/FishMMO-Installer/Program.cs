using FishMMO.Database;
using Microsoft.Extensions.Configuration;

namespace FishMMO.Installer
{
	/// <summary>
	/// Console-based installer tool for FishMMO dependencies and database setup.
	/// Delegates all work to focused installer classes: <see cref="DotNetInstaller"/>,
	/// <see cref="PgBouncerInstaller"/>,
	/// <see cref="PostgreSQLInstaller"/>, <see cref="NGINXInstaller"/>,
	/// <see cref="VSBuildToolsInstaller"/>, <see cref="UnityInstaller"/>, <see cref="LetsEncryptInstaller"/>,
	/// and <see cref="ProjectBuildInstaller"/>.
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// Stores the loaded application settings from appsettings.json.
		/// </summary>
		private static AppSettings appSettings = new AppSettings();
		private static DatabaseProvider activeProvider = DatabaseProvider.PostgreSql;

		/// <summary>
		/// Entry point. Loads appsettings.json and runs the installer menu loop.
		/// </summary>
		public static async Task Main(string[] args)
		{
			// Normalize environment selection once and propagate to standard variables.
			string environmentName = DatabaseConfigurationHelper.ResolveEnvironmentName();

			Environment.SetEnvironmentVariable("FISHMMO_ENVIRONMENT", environmentName);
			Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", environmentName);
			Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);

			LoadAppSettings(environmentName);
			activeProvider = DatabaseConfigurationHelper.ResolveDatabaseProvider(new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Database:Provider"] = appSettings.Database?.Provider
				})
				.Build());
			await RunMenuLoop();
		}

		/// <summary>
		/// Loads application settings using ConfigurationBuilder.
		/// appsettings.json is treated as the default source and optional
		/// appsettings.{Environment}.json overlays values when an environment is provided.
		/// </summary>
		private static void LoadAppSettings(string environmentName)
		{
			try
			{
				IConfiguration configuration = DatabaseConfigurationHelper.BuildDesignTimeConfiguration();

				appSettings = configuration.Get<AppSettings>() ?? new AppSettings();

				InstallerProcessHelper.Log($"Configuration successfully loaded for Environment: {environmentName}");
			}
			catch (Exception ex)
			{
				InstallerProcessHelper.Log($"Critical error loading configuration: {ex.Message}");
				appSettings = new AppSettings();
			}
		}

		/// <summary>
		/// Runs the interactive console menu loop until the user quits.
		/// </summary>
		private static async Task RunMenuLoop()
		{
			while (true)
			{
				Console.Clear();
				Console.WriteLine("Welcome to the FishMMO Installer Tool.");
				Console.WriteLine($"Active DB Provider: {activeProvider}");
				Console.WriteLine("Press a key (0-9, A-D):");
				Console.WriteLine("1 : Install DotNet");
				Console.WriteLine("2 : Install Visual Studio Build Tools (Windows Only)");
				Console.WriteLine("3 : Install PgBouncer (Connection Pooler)");
				Console.WriteLine("4 : Build all C# Projects");
				Console.WriteLine("5 : Install Unity Hub");
				Console.WriteLine("6 : Install Unity Editor (+Modules)");
				Console.WriteLine("7 : Install NGINX (Web Server/Reverse Proxy)");
				Console.WriteLine("8 : Install/Renew Let's Encrypt Certificate (NGINX)");
				Console.WriteLine("9 : Install PostgreSQL (Database Server)");
				Console.WriteLine("A : Install FishMMO Database (User/Schema/Initial Migration)");
				Console.WriteLine("B : Create new database migration");
				Console.WriteLine("C : Grant User Permissions on Database");
				Console.WriteLine("D : Delete FishMMO Database (DANGEROUS!)");
				Console.WriteLine("E : Switch Active Database Provider (PostgreSql/SqlServer)");
				Console.WriteLine("0 : Quit");

				ConsoleKeyInfo key = Console.ReadKey(true);

				switch (key.Key)
				{
					case ConsoleKey.D1:
						await DotNetInstaller.InstallDotNet();
						break;
					case ConsoleKey.D2:
						await VSBuildToolsInstaller.InstallVSBuildTools();
						break;
					case ConsoleKey.D3:
						await PgBouncerInstaller.InstallPgBouncer(appSettings);
						break;
					case ConsoleKey.D4:
						await ProjectBuildInstaller.BuildAllProjectsInSelectedRootAsync();
						break;
					case ConsoleKey.D5:
						await UnityInstaller.InstallUnityHub();
						break;
					case ConsoleKey.D6:
						await UnityInstaller.InstallUnityVersion();
						break;
					case ConsoleKey.D7:
						await NGINXInstaller.InstallNGINX();
						break;
					case ConsoleKey.D8:
						await LetsEncryptInstaller.InstallLetsEncryptCertificate();
						break;
					case ConsoleKey.D9:
						if (activeProvider == DatabaseProvider.SqlServer)
						{
							await HandleWithSettings(s => s.SqlServer?.Server, "SqlServer server", SqlServerInstaller.InstallSqlServer);
						}
						else
						{
							await HandleWithSettings(s => s.Npgsql?.Host, "Npgsql host", PostgreSQLInstaller.InstallPostgreSQL);
						}
						break;
					case ConsoleKey.A:
						if (activeProvider == DatabaseProvider.SqlServer)
						{
							await HandleWithSettings(s => s.SqlServer?.Database, "SqlServer database", SqlServerInstaller.InstallFishMMODatabase);
						}
						else
						{
							await HandleWithSuperuser(s => s.Npgsql?.Database, "Npgsql database", PostgreSQLInstaller.InstallFishMMODatabase);
						}
						break;
					case ConsoleKey.B:
						await CreateMigrationForActiveProvider();
						break;
					case ConsoleKey.C:
						await HandleWithSuperuser(
							s => s.Npgsql?.Username,
							"Npgsql database/username",
							PostgreSQLInstaller.GrantUserPermissions);
						break;
					case ConsoleKey.D:
						if (activeProvider == DatabaseProvider.PostgreSql)
						{
							await HandleWithSuperuser(s => s.Npgsql?.Database, "Npgsql database", PostgreSQLInstaller.DeleteFishMMODatabase);
						}
						else
						{
							InstallerProcessHelper.Log("SQL Server delete is not automated yet. Drop the database manually if required.");
						}
						break;
					case ConsoleKey.E:
						activeProvider = activeProvider == DatabaseProvider.PostgreSql ? DatabaseProvider.SqlServer : DatabaseProvider.PostgreSql;
						InstallerProcessHelper.Log($"Switched active provider to {activeProvider}.");
						break;
					case ConsoleKey.D0:
						return;
					default:
						Console.WriteLine("Invalid input. Please enter a valid option.");
						break;
				}

				Console.WriteLine("Press any key to continue...");
				Console.ReadKey(true);
			}
		}

		private static async Task CreateMigrationForActiveProvider()
		{
			string? migrationName = InstallerProcessHelper.PromptForInput("Enter a name for the new migration (e.g., 'AddPlayerInventory'): ");
			if (string.IsNullOrWhiteSpace(migrationName))
			{
				InstallerProcessHelper.Log("Migration name cannot be empty.");
				return;
			}

			bool migrationSuccess = await DotNetInstaller.RunEFMigrationAsync(migrationName, activeProvider);
			if (!migrationSuccess)
			{
				InstallerProcessHelper.Log($"Failed to create migration '{migrationName}' for provider '{activeProvider}'.");
				return;
			}

			bool updateSuccess = await DotNetInstaller.RunEFDatabaseUpdateAsync(activeProvider);
			InstallerProcessHelper.Log(updateSuccess
				? $"Migration '{migrationName}' applied for provider '{activeProvider}'."
				: $"Migration '{migrationName}' created but not applied for provider '{activeProvider}'.");
		}

		/// <summary>
		/// Validates that the required Npgsql setting is present, then delegates to the handler.
		/// </summary>
		/// <param name="requiredField">Selector for the required field to validate.</param>
		/// <param name="fieldDescription">Human-readable name of the required field for error messages.</param>
		/// <param name="handler">Async action receiving the validated app settings.</param>
		private static async Task HandleWithSettings(
			Func<AppSettings, string?> requiredField,
			string fieldDescription,
			Func<AppSettings, Task> handler)
		{
			if (string.IsNullOrWhiteSpace(requiredField(appSettings)))
			{
				InstallerProcessHelper.Log($"appsettings.json is not loaded or {fieldDescription} is not defined. Cannot proceed without configuration.");
				return;
			}
			await handler(appSettings);
		}

		/// <summary>
		/// Validates settings, prompts for superuser credentials, then delegates to the handler.
		/// </summary>
		/// <param name="requiredField">Selector for the required field to validate.</param>
		/// <param name="fieldDescription">Human-readable name of the required field for error messages.</param>
		/// <param name="handler">Async action receiving (superUsername, superPassword, appSettings).</param>
		private static async Task HandleWithSuperuser(
			Func<AppSettings, string?> requiredField,
			string fieldDescription,
			Func<string, string, AppSettings, Task> handler)
		{
			if (string.IsNullOrWhiteSpace(requiredField(appSettings)))
			{
				InstallerProcessHelper.Log($"appsettings.json is not loaded or {fieldDescription} is not defined. Cannot proceed without configuration.");
				return;
			}
			string superUsername = InstallationConstants.PostgreSQLDefaultSuperuser;
			string superPassword = InstallerProcessHelper.PromptForPassword($"Enter PostgreSQL Superuser Password (username is '{superUsername}'): ");
			await handler(superUsername, superPassword, appSettings);
		}
	}
}
