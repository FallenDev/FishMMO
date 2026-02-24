using FishMMO.Database;
using Microsoft.Extensions.Configuration;

namespace FishMMO.Installer
{
	/// <summary>
	/// Console-based installer tool for FishMMO dependencies and SqlServer database setup.
	/// </summary>
	public static class Program
	{
		private static AppSettings appSettings = new AppSettings();
		private static DatabaseProvider activeProvider = DatabaseProvider.SqlServer;

		public static async Task Main(string[] args)
		{
			string environmentName = DatabaseConfigurationHelper.ResolveEnvironmentName();

			Environment.SetEnvironmentVariable("FISHMMO_ENVIRONMENT", environmentName);
			Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", environmentName);
			Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);
			Environment.SetEnvironmentVariable("Database__Provider", nameof(DatabaseProvider.SqlServer));

			LoadAppSettings(environmentName);
			activeProvider = DatabaseConfigurationHelper.ResolveDatabaseProvider(new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Database:Provider"] = appSettings.Database?.Provider
				})
				.Build());
			await RunMenuLoop();
		}

		private static void LoadAppSettings(string environmentName)
		{
			try
			{
				IConfiguration configuration = DatabaseConfigurationHelper.BuildDesignTimeConfiguration();
				appSettings = configuration.Get<AppSettings>() ?? new AppSettings();
				appSettings.Database.Provider = nameof(DatabaseProvider.SqlServer);
				InstallerProcessHelper.Log($"Configuration successfully loaded for Environment: {environmentName}");
			}
			catch (Exception ex)
			{
				InstallerProcessHelper.Log($"Critical error loading configuration: {ex.Message}");
				appSettings = new AppSettings();
			}
		}

		private static async Task RunMenuLoop()
		{
			while (true)
			{
				Console.Clear();
				Console.WriteLine("Welcome to the FishMMO Installer Tool (SqlServer mode).");
				Console.WriteLine("Press a key (0-9, A-B):");
				Console.WriteLine("1 : Install DotNet");
				Console.WriteLine("2 : Install Visual Studio Build Tools (Windows Only)");
				Console.WriteLine("3 : Build all C# Projects");
				Console.WriteLine("4 : Install Unity Hub");
				Console.WriteLine("5 : Install Unity Editor (+Modules)");
				Console.WriteLine("6 : Install NGINX (Web Server/Reverse Proxy)");
				Console.WriteLine("7 : Install/Renew Let's Encrypt Certificate (NGINX)");
				Console.WriteLine("8 : Validate SqlServer connectivity");
				Console.WriteLine("9 : Provision FishMMO SqlServer Database");
				Console.WriteLine("A : Create new database migration");
				Console.WriteLine("B : Apply pending migrations");
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
						await ProjectBuildInstaller.BuildAllProjectsInSelectedRootAsync();
						break;
					case ConsoleKey.D4:
						await UnityInstaller.InstallUnityHub();
						break;
					case ConsoleKey.D5:
						await UnityInstaller.InstallUnityVersion();
						break;
					case ConsoleKey.D6:
						await NGINXInstaller.InstallNGINX();
						break;
					case ConsoleKey.D7:
						await LetsEncryptInstaller.InstallLetsEncryptCertificate();
						break;
					case ConsoleKey.D8:
						await HandleWithSettings(s => s.SqlServer?.Server, "SqlServer server", SqlServerInstaller.InstallSqlServer);
						break;
					case ConsoleKey.D9:
						await HandleWithSettings(s => s.SqlServer?.Database, "SqlServer database", SqlServerInstaller.InstallFishMMODatabase);
						break;
					case ConsoleKey.A:
						await CreateMigration();
						break;
					case ConsoleKey.B:
						await ApplyMigrations();
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

		private static async Task CreateMigration()
		{
			string? migrationName = InstallerProcessHelper.PromptForInput("Enter a name for the new migration (e.g., 'AddPlayerInventory'): ");
			if (string.IsNullOrWhiteSpace(migrationName))
			{
				InstallerProcessHelper.Log("Migration name cannot be empty.");
				return;
			}

			bool migrationSuccess = await DotNetInstaller.RunEFMigrationAsync(migrationName);
			if (!migrationSuccess)
			{
				InstallerProcessHelper.Log($"Failed to create migration '{migrationName}'.");
				return;
			}

			InstallerProcessHelper.Log($"Migration '{migrationName}' created successfully.");
		}

		private static async Task ApplyMigrations()
		{
			bool updateSuccess = await DotNetInstaller.RunEFDatabaseUpdateAsync();
			InstallerProcessHelper.Log(updateSuccess ? "Migrations applied successfully." : "Failed to apply migrations.");
		}

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
	}
}
