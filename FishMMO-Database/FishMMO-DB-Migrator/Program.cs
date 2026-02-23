using FishMMO.Database;
using FishMMO.Database.Npgsql;
using FishMMO.Database.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FishMMO.Database.Migrator;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		try
		{
			IConfiguration configuration = DatabaseConfigurationHelper.BuildDesignTimeConfiguration();
			DatabaseProvider provider = DatabaseConfigurationHelper.ResolveDatabaseProvider(configuration);

			await using var factory = provider == DatabaseProvider.SqlServer
				? (INpgsqlDbContextFactory)new SqlServerDbContextFactory(configuration)
				: new NpgsqlDbContextFactory(configuration);

			await using NpgsqlDbContext dbContext = await factory.CreateDbContextAsync();
			await dbContext.Database.MigrateAsync();

			Console.WriteLine($"Migrations applied successfully using provider '{provider}'.");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Migration failed: {ex.Message}");
			return 1;
		}
	}
}
