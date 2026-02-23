using System;
using FishMMO.Database.Exceptions;
using FishMMO.Database.Npgsql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FishMMO.Database.SqlServer
{
	/// <summary>
	/// Encapsulates validated SQL Server database configuration.
	/// </summary>
	public sealed class SqlServerDbConfiguration
	{
		public SqlServerSettings Settings { get; }
		public string ConnectionString { get; }
		public bool EnableLogging { get; }
		public int MaxPoolSize => Settings.MaxPoolSize;
		public int CommandTimeout => Settings.CommandTimeout;
		public RetryPolicyConfiguration RetryPolicy => Settings.RetryPolicy ?? new RetryPolicyConfiguration();
		public FishMMO.Database.Npgsql.Monitoring.Diagnostics.QueryPerformanceConfiguration PerformanceConfiguration { get; }

		public SqlServerDbConfiguration(IConfiguration configuration, bool enableLogging = false, int? commandTimeoutOverride = null)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));

			EnableLogging = enableLogging;
			Settings = configuration.GetSection("SqlServer").Get<SqlServerSettings>() ?? new SqlServerSettings();

			if (commandTimeoutOverride.HasValue)
				Settings.CommandTimeout = commandTimeoutOverride.Value;

			ValidateRequired("SqlServer:Server", Settings.Server);
			ValidateIdentifier("SqlServer:Database", Settings.Database);
			ValidateRequired("SqlServer:Username", Settings.Username);
			ValidateRequired("SqlServer:Password", Settings.Password);
			ValidateRange("SqlServer:ConnectionTimeout", Settings.ConnectionTimeout, 1);
			ValidateRange("SqlServer:CommandTimeout", Settings.CommandTimeout, 1);
			ValidateRange("SqlServer:MinPoolSize", Settings.MinPoolSize, 0);
			ValidateRange("SqlServer:MaxPoolSize", Settings.MaxPoolSize, 1);

			if (Settings.MinPoolSize > Settings.MaxPoolSize)
			{
				throw new DatabaseException("SqlServer:MinPoolSize cannot be greater than SqlServer:MaxPoolSize.", errorCode: DatabaseErrorCodes.InvalidConfiguration);
			}

			ConnectionString = BuildConnectionString(Settings);
			PerformanceConfiguration = MapPerformanceConfiguration(Settings.QueryPerformanceTracking);
		}

		private static string BuildConnectionString(SqlServerSettings settings)
		{
			return new SqlConnectionStringBuilder
			{
				DataSource = settings.Server,
				InitialCatalog = settings.Database,
				UserID = settings.Username,
				Password = settings.Password,
				TrustServerCertificate = settings.TrustServerCertificate,
				ConnectTimeout = settings.ConnectionTimeout,
				MinPoolSize = settings.MinPoolSize,
				MaxPoolSize = settings.MaxPoolSize,
				PersistSecurityInfo = false,
				Encrypt = true
			}.ConnectionString;
		}

		private static FishMMO.Database.Npgsql.Monitoring.Diagnostics.QueryPerformanceConfiguration MapPerformanceConfiguration(global::FishMMO.Database.QueryPerformanceConfiguration source)
		{
			source ??= new global::FishMMO.Database.QueryPerformanceConfiguration();
			return new FishMMO.Database.Npgsql.Monitoring.Diagnostics.QueryPerformanceConfiguration
			{
				Enabled = source.Enabled,
				Level = source.Level,
				SlowQueryThresholdMs = source.SlowQueryThresholdMs,
				SampleRate = source.SampleRate
			};
		}

		private static void ValidateRequired(string settingPath, string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new DatabaseException($"Invalid configuration value for '{settingPath}'.", errorCode: DatabaseErrorCodes.InvalidConfiguration);
			}
		}

		private static void ValidateIdentifier(string settingPath, string value)
		{
			if (string.IsNullOrWhiteSpace(value) || !DbContextExtensions.IsValidUnquotedIdentifier(value))
			{
				throw new DatabaseException($"Invalid configuration value for '{settingPath}': '{value}'. Must be snake_case.", errorCode: DatabaseErrorCodes.InvalidConfiguration);
			}
		}

		private static void ValidateRange(string settingPath, int value, int minInclusive)
		{
			if (value < minInclusive)
			{
				throw new DatabaseException($"Invalid configuration value for '{settingPath}': '{value}'.", errorCode: DatabaseErrorCodes.InvalidConfiguration);
			}
		}
	}
}
