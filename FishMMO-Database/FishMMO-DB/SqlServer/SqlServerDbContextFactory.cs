using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FishMMO.Database.SqlServer;
using FishMMO.Database.SqlServer.Monitoring.Diagnostics;
using FishMMO.Database.SqlServer.Monitoring.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FishMMO.Database.SqlServer
{
	/// <summary>
	/// SqlServer-backed implementation of <see cref="ISqlServerDbContextFactory"/>.
	/// </summary>
	public sealed class SqlServerDbContextFactory : ISqlServerDbContextFactory
	{
		private const int DisposeWaitTimeoutMs = 5000;
		private const int ShutdownPollIntervalMs = 50;

		private int disposed;
		private int shutdown;
		private int activeContextCount;
		private readonly SqlServerDbConfiguration configuration;
		private readonly DbContextOptions<SqlServerDbContext> cachedOptions;
		private readonly ConnectionPoolMetrics poolMetrics;
		private readonly QueryPerformanceTracker performanceTracker;

		public SqlServerDbContextFactory(IConfiguration configuration) : this(new SqlServerDbConfiguration(configuration)) { }
		public SqlServerDbContextFactory(IConfiguration configuration, bool enableLogging) : this(new SqlServerDbConfiguration(configuration, enableLogging)) { }
		public SqlServerDbContextFactory(IConfiguration configuration, bool enableLogging, int commandTimeout) : this(new SqlServerDbConfiguration(configuration, enableLogging, commandTimeout)) { }

		public SqlServerDbContextFactory(SqlServerDbConfiguration configuration)
		{
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			poolMetrics = new ConnectionPoolMetrics();
			var connectionMetricsInterceptor = new ConnectionMetricsInterceptor(poolMetrics);

			var optionsBuilder = new DbContextOptionsBuilder<SqlServerDbContext>()
				.UseSqlServer(configuration.ConnectionString, sqlOptions =>
				{
					sqlOptions.CommandTimeout(configuration.CommandTimeout);
				})
				.UseSnakeCaseNamingConvention()
				.AddInterceptors(connectionMetricsInterceptor);

			if (configuration.EnableLogging)
				optionsBuilder.EnableSensitiveDataLogging(true);

			cachedOptions = optionsBuilder.Options;
			performanceTracker = new QueryPerformanceTracker(configuration.PerformanceConfiguration);
		}

		public ConnectionPoolMetrics PoolMetrics => poolMetrics;
		public int MaxPoolSize => configuration.MaxPoolSize;
		public QueryPerformanceTracker PerformanceTracker => performanceTracker;
		public RetryPolicyConfiguration RetryPolicy => configuration.RetryPolicy;
		public int ActiveContextCount => Volatile.Read(ref activeContextCount);

		public SqlServerDbContext CreateDbContext()
		{
			ThrowIfDisposedOrShutdown();
			Interlocked.Increment(ref activeContextCount);
			var context = new SqlServerDbContext(cachedOptions, SqlServerDbContext.DefaultSchema);
			context.Disposed += OnContextDisposed;
			return context;
		}

		public async Task<SqlServerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return await Task.FromResult(CreateDbContext()).ConfigureAwait(false);
		}

		public void Shutdown()
		{
			if (Interlocked.Exchange(ref shutdown, 1) != 0)
				return;
		}

		public Task ShutdownAsync(CancellationToken cancellationToken = default)
		{
			Shutdown();
			return Task.CompletedTask;
		}

		public async Task<bool> ShutdownGracefullyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
		{
			Shutdown();
			var elapsed = Stopwatch.StartNew();
			while (Volatile.Read(ref activeContextCount) > 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (elapsed.Elapsed >= timeout)
					return false;
				await Task.Delay(ShutdownPollIntervalMs, cancellationToken).ConfigureAwait(false);
			}
			return true;
		}

		public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				using var context = CreateDbContext();
				return await context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				return false;
			}
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref disposed, 1) != 0)
				return;

			Shutdown();
			var elapsed = Stopwatch.StartNew();
			while (Volatile.Read(ref activeContextCount) > 0 && elapsed.ElapsedMilliseconds < DisposeWaitTimeoutMs)
				Thread.Sleep(ShutdownPollIntervalMs);

			performanceTracker.Dispose();
			poolMetrics.Reset();
			GC.SuppressFinalize(this);
		}

		public async ValueTask DisposeAsync()
		{
			if (Interlocked.Exchange(ref disposed, 1) != 0)
				return;

			Shutdown();
			var elapsed = Stopwatch.StartNew();
			while (Volatile.Read(ref activeContextCount) > 0 && elapsed.ElapsedMilliseconds < DisposeWaitTimeoutMs)
				await Task.Delay(ShutdownPollIntervalMs).ConfigureAwait(false);

			performanceTracker.Dispose();
			poolMetrics.Reset();
			GC.SuppressFinalize(this);
		}

		private void OnContextDisposed(object? sender, EventArgs e)
		{
			if (sender is SqlServerDbContext dbContext)
				dbContext.Disposed -= OnContextDisposed;
			Interlocked.Decrement(ref activeContextCount);
		}

		private void ThrowIfDisposedOrShutdown()
		{
			if (Volatile.Read(ref disposed) != 0 || Volatile.Read(ref shutdown) != 0)
				throw new ObjectDisposedException(nameof(SqlServerDbContextFactory), "SqlServerDbContextFactory has been shut down.");
		}
	}
}
