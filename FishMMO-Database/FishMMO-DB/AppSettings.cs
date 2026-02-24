using System;
using FishMMO.Database.SqlServer.Monitoring.Diagnostics;

namespace FishMMO.Database
{
	/// <summary>
	/// Application settings for database connections.
	/// </summary>
	[Serializable]
	public class AppSettings
	{
		/// <summary>
		/// Gets or sets common database provider selection settings.
		/// </summary>
		public DatabaseSettings Database { get; set; } = new();

		/// <summary>
		/// Gets or sets SqlServer connection settings.
		/// </summary>
		public SqlServerSettings SqlServer { get; set; } = new();

		/// <summary>
		/// Gets or sets Redis connection settings.
		/// </summary>
		public RedisSettings Redis { get; set; } = new();
	}

	/// <summary>
	/// Common database settings.
	/// </summary>
	[Serializable]
	public class DatabaseSettings
	{
		/// <summary>
		/// Gets or sets the active database provider.
		/// </summary>
		public string Provider { get; set; } = nameof(DatabaseProvider.SqlServer);
	}

	/// <summary>
	/// SqlServer connection settings.
	/// </summary>
	[Serializable]
	public class SqlServerSettings
	{
		public string Server { get; set; } = "127.0.0.1,1433";
		public string Database { get; set; } = "fish_mmo_sqlserver";
		public string Username { get; set; } = "sa";
		public string Password { get; set; } = "pass";
		public bool TrustServerCertificate { get; set; } = true;
		public int CommandTimeout { get; set; } = 10;
		public int ConnectionTimeout { get; set; } = 15;
		public int MinPoolSize { get; set; } = 5;
		public int MaxPoolSize { get; set; } = 100;
		public QueryPerformanceConfiguration QueryPerformanceTracking { get; set; } = new();
		public RetryPolicyConfiguration RetryPolicy { get; set; } = new();
	}

	/// <summary>
	/// Configuration for tracking and logging slow database queries.
	/// </summary>
	[Serializable]
	public class QueryPerformanceConfiguration
	{
		/// <summary>
		/// Gets or sets whether query performance tracking is enabled.
		/// </summary>
		public bool Enabled { get; set; } = false;

		/// <summary>
		/// Gets or sets the tracking verbosity level.
		/// </summary>
		public TrackingLevel Level { get; set; } = TrackingLevel.Basic;

		/// <summary>
		/// Gets or sets the threshold in milliseconds for classifying queries as slow.
		/// </summary>
		public double SlowQueryThresholdMs { get; set; } = 100.0;

		/// <summary>
		/// Gets or sets sampling rate for tracked queries in range 0..1.
		/// </summary>
		public double SampleRate { get; set; } = 1.0;
	}

	/// <summary>
	/// Configuration for handling database connection retries.
	/// </summary>
	[Serializable]
	public class RetryPolicyConfiguration
	{
		/// <summary>
		/// Gets or sets the maximum retry attempts for transient failures.
		/// </summary>
		public int MaxRetries { get; set; } = 3;

		/// <summary>
		/// Gets or sets the base delay in milliseconds between retries.
		/// </summary>
		public int BaseDelayMs { get; set; } = 1000;

		/// <summary>
		/// Gets or sets the maximum random jitter in milliseconds added to retry delays.
		/// </summary>
		public int MaxJitterMs { get; set; } = 100;
	}

	/// <summary>
	/// Redis connection settings.
	/// </summary>
	[Serializable]
	public class RedisSettings
	{
		/// <summary>
		/// Gets or sets the Redis host.
		/// </summary>
		public string Host { get; set; } = "127.0.0.1";

		/// <summary>
		/// Gets or sets the Redis port.
		/// </summary>
		public string Port { get; set; } = "6379";

		/// <summary>
		/// Gets or sets the Redis password.
		/// </summary>
		public string Password { get; set; }
	}
}
