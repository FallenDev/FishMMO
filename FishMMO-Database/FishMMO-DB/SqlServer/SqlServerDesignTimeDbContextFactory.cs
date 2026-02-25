using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FishMMO.Database.SqlServer
{
	/// <summary>
	/// EF Core design-time factory used by dotnet-ef to instantiate <see cref="SqlServerDbContext"/>.
	/// </summary>
	public sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlServerDbContext>
	{
		public SqlServerDbContext CreateDbContext(string[] args)
		{
			string basePath = ResolveConfigurationBasePath();
			string environment = DatabaseConfigurationHelper.ResolveEnvironmentName();

			IConfiguration configuration = new ConfigurationBuilder()
				.SetBasePath(basePath)
				.AddJsonFile("appsettings.json", optional: true)
				.AddJsonFile($"appsettings.{environment}.json", optional: true)
				.AddEnvironmentVariables()
				.Build();

			var dbConfiguration = new SqlServerDbConfiguration(configuration);
			var optionsBuilder = new DbContextOptionsBuilder<SqlServerDbContext>()
				.UseSqlServer(dbConfiguration.ConnectionString, sqlOptions =>
				{
					sqlOptions.CommandTimeout(dbConfiguration.CommandTimeout);
				});

			return new SqlServerDbContext(optionsBuilder.Options, SqlServerDbContext.DefaultSchema);
		}

		private static string ResolveConfigurationBasePath()
		{
			string[] candidates =
			[
				Directory.GetCurrentDirectory(),
				AppContext.BaseDirectory,
				Path.Combine(AppContext.BaseDirectory, "FishMMO-Database", "FishMMO-DB"),
				Path.Combine(Directory.GetCurrentDirectory(), "FishMMO-Database", "FishMMO-DB")
			];

			foreach (string candidate in candidates)
			{
				if (string.IsNullOrWhiteSpace(candidate))
					continue;

				if (File.Exists(Path.Combine(candidate, "appsettings.json")))
					return candidate;
			}

			return Directory.GetCurrentDirectory();
		}
	}
}
