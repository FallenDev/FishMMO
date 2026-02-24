using System;
using System.Threading.Tasks;

using FishMMO.Database;
using FishMMO.Database.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace FishMMO.Database.Migrator;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		try
		{
			Environment.SetEnvironmentVariable("Database__Provider", "SqlServer");
			await using var factory = new SqlServerDbContextFactory(DatabaseConfigurationHelper.BuildDesignTimeConfiguration());
			await using var dbContext = await factory.CreateDbContextAsync();
			await dbContext.Database.MigrateAsync();

			Console.WriteLine("SqlServer migrations applied successfully.");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Migration failed: {ex.Message}");
			return 1;
		}
	}
}
