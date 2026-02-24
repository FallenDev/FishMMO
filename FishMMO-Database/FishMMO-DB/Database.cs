using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using FishMMO.Database.SqlServer.Monitoring.Health;
using FishMMO.Database.SqlServer.Monitoring.Metrics;
using FishMMO.Database.SqlServer.Monitoring.Diagnostics;
using FishMMO.Database.Exceptions;
using FishMMO.Database.SqlServer;

namespace FishMMO.Database
{
	/// <summary>
	/// Main orchestrator and facade for the FishMMO database layer.
	/// Provides centralized access to database services, health monitoring, and metrics.
	/// This class initializes the database context factory, discovers and registers concrete service
	/// implementations, and exposes monitoring points to the host application.
	/// </summary>
	public sealed class Database : IDatabase
	{
		/// <inheritdoc/>
		/// <remarks>
		/// The service registry is populated during construction by discovering implementations
		/// in the `FishMMO.Database.SqlServer.Services` assembly. Consumers should call
		/// <see cref="IDatabaseServiceRegistry.TryGet{TService}(out TService)"/> to obtain services.
		/// </remarks>
		public IDatabaseServiceRegistry ServiceRegistry { get; private set; }

		/// <inheritdoc/>
		/// <remarks>
		/// The <see cref="DatabaseHealthMonitor"/> performs lightweight connectivity and response-time checks
		/// against the underlying database using <see cref="ISqlServerDbContextFactory"/>.
		/// </remarks>
		public DatabaseHealthMonitor HealthMonitor { get; private set; }

		/// <inheritdoc/>
		public DatabaseMetricsTracker MetricsTracker { get; private set; }

		/// <inheritdoc/>
		/// <remarks>
		/// Exposes the configured <see cref="ISqlServerDbContextFactory"/> used to create short-lived
		/// <see cref="SqlServerDbContext"/> instances. Prefer service interfaces from <see cref="ServiceRegistry"/>
		/// for application operations.
		/// </remarks>
		public ISqlServerDbContextFactory DbContextFactory { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Database"/> class with a pre-built <see cref="IConfiguration"/>.
		/// Creates the DbContext factory, discovers and registers all services, and sets up monitoring.
		/// </summary>
		/// <param name="configuration">Application configuration containing an <c>SqlServer</c> section. Cannot be <c>null</c>.</param>
		/// <param name="enableLogging">Enable sensitive data logging for development (default: false).</param>
		/// <param name="commandTimeout">Optional database command timeout override in seconds.</param>
		/// <param name="healthCheckWarningMs">Health check warning threshold in milliseconds (default: 100).</param>
		/// <param name="healthCheckCriticalMs">Health check critical threshold in milliseconds (default: 500).</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is <c>null</c>.</exception>
		/// <exception cref="DatabaseException">Thrown when initialization fails due to configuration or registration issues.</exception>
		public Database(
			IConfiguration configuration,
			bool enableLogging = false,
			int? commandTimeout = null,
			int healthCheckWarningMs = 100,
			int healthCheckCriticalMs = 500)
		{
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			Initialize(
				new SqlServerDbContextFactory(new SqlServerDbConfiguration(configuration, enableLogging, commandTimeout)),
				healthCheckWarningMs,
				healthCheckCriticalMs);
		}

		/// <summary>
		/// Shared initialization logic used by constructors. Sets up the DbContext factory, service registry,
		/// health monitor and metrics tracker.
		/// </summary>
		/// <param name="dbContextFactory">The pre-configured <see cref="ISqlServerDbContextFactory"/> to use. Cannot be <c>null</c>.</param>
		/// <param name="healthCheckWarningMs">Health check warning threshold in milliseconds.</param>
		/// <param name="healthCheckCriticalMs">Health check critical threshold in milliseconds.</param>
		/// <exception cref="DatabaseException">Thrown when initialization fails.</exception>
		private void Initialize(
			ISqlServerDbContextFactory dbContextFactory,
			int healthCheckWarningMs,
			int healthCheckCriticalMs)
		{
			try
			{
				DbContextFactory = dbContextFactory;
				ServiceRegistry = CreateSqlServerServiceRegistry(DbContextFactory);
				HealthMonitor = new DatabaseHealthMonitor(
					DbContextFactory,
					healthCheckWarningMs,
					healthCheckCriticalMs);
				MetricsTracker = new DatabaseMetricsTracker();
			}
			catch (Exception ex)
			{
				throw new DatabaseException(
					"Failed to initialize database orchestrator.",
					ex,
					"INVALID_CONFIGURATION");
			}
		}

		private IDatabaseServiceRegistry CreateSqlServerServiceRegistry(ISqlServerDbContextFactory dbContextFactory)
		{
			if (dbContextFactory == null)
				throw new ArgumentNullException(nameof(dbContextFactory));

			var registry = new SqlServerServiceRegistry();
			RegisterServicesByReflection(registry, dbContextFactory);
			return registry;
		}

		/// <summary>
		/// Creates and populates an <see cref="SqlServerServiceRegistry"/> by reflecting over the
		/// SqlServer services assembly and constructing concrete implementations.
		/// </summary>
		/// <param name="dbContextFactory">DbContext factory passed to service constructors.</param>
		/// <returns>An initialized <see cref="IDatabaseServiceRegistry"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContextFactory"/> is <c>null</c>.</exception>
		/// <summary>
		/// Discovers and registers SqlServer-backed service implementations by reflection.
		///
		/// The method finds all service interfaces in the namespace
		/// <c>FishMMO.Database.SqlServer.Services.Interfaces</c> and pairs them with a single concrete
		/// implementation in <c>FishMMO.Database.SqlServer.Services</c>. Each implementation is constructed
		/// with the provided <see cref="ISqlServerDbContextFactory"/> and registered into the registry.
		/// </summary>
		/// <param name="registry">The registry to populate. Cannot be <c>null</c>.</param>
		/// <param name="dbContextFactory">Factory instance to pass to service constructors. Cannot be <c>null</c>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> or <paramref name="dbContextFactory"/> is <c>null</c>.</exception>
		/// <exception cref="DatabaseException">Thrown when service discovery or construction fails.</exception>
		private static void RegisterServicesByReflection(IDatabaseServiceRegistry registry, ISqlServerDbContextFactory dbContextFactory)
		{
			if (registry == null)
				throw new ArgumentNullException(nameof(registry));
			if (dbContextFactory == null)
				throw new ArgumentNullException(nameof(dbContextFactory));

			// Discover all service interfaces and their concrete implementations, then register them.
			// This avoids a growing manual registration wall as new tables/services are added.
			const string interfaceNamespace = "FishMMO.Database.SqlServer.Services.Interfaces";

			var assembly = typeof(SqlServerDbContextFactory).Assembly;
			var serviceInterfaces = assembly.GetTypes()
				.Where(t => t.IsInterface
					&& string.Equals(t.Namespace, interfaceNamespace, StringComparison.Ordinal)
					&& t.Name.EndsWith("Service", StringComparison.Ordinal))
				.OrderBy(t => t.FullName, StringComparer.Ordinal)
				.ToArray();

			var candidates = assembly.GetTypes()
				.Where(t => t.IsClass
					&& !t.IsAbstract
					&& t.Namespace != null
					&& t.Namespace.StartsWith("FishMMO.Database.SqlServer.Services", StringComparison.Ordinal))
				.ToArray();

			var registerOpenMethod = registry.GetType().GetMethod(
				"Register",
				BindingFlags.Instance | BindingFlags.NonPublic);
			if (registerOpenMethod == null)
			{
				throw new DatabaseException(
					"Failed to locate service registry registration method for dynamic registration.",
					errorCode: "INVALID_CONFIGURATION");
			}

			var interfaceToImplementation = serviceInterfaces
				.Select(serviceInterface =>
				{
					var implementations = candidates
						.Where(t => serviceInterface.IsAssignableFrom(t))
						.ToArray();

					if (implementations.Length == 0)
					{
						throw new DatabaseException(
							$"No implementation found for service interface '{serviceInterface.FullName}'.",
							errorCode: "INVALID_CONFIGURATION");
					}

					if (implementations.Length > 1)
					{
						var implList = string.Join(", ", implementations.Select(t => t.FullName).OrderBy(n => n, StringComparer.Ordinal));
						throw new DatabaseException(
							$"Multiple implementations found for service interface '{serviceInterface.FullName}': {implList}." +
							" Please keep exactly one implementation per service interface.",
							errorCode: "INVALID_CONFIGURATION");
					}

					return new { ServiceInterface = serviceInterface, Implementation = implementations[0] };
				})
				.ToArray();

			var groupedByImplementation = interfaceToImplementation
				.GroupBy(x => x.Implementation, x => x.ServiceInterface)
				.OrderBy(g => g.Key.FullName, StringComparer.Ordinal)
				.ToArray();

			foreach (var group in groupedByImplementation)
			{
				var implementation = group.Key;
				object instance;

				try
				{
					instance = Activator.CreateInstance(implementation, dbContextFactory)!;
				}
				catch (TargetInvocationException tie)
				{
					var inner = tie.InnerException ?? tie;
					var serviceInterfacesList = string.Join(", ", group.Select(t => t.FullName).OrderBy(n => n, StringComparer.Ordinal));
					throw new DatabaseException(
						$"Failed to construct '{implementation.FullName}' for: {serviceInterfacesList}.",
						innerException: inner,
						errorCode: "INVALID_CONFIGURATION");
				}
				catch (Exception ex)
				{
					var serviceInterfacesList = string.Join(", ", group.Select(t => t.FullName).OrderBy(n => n, StringComparer.Ordinal));
					throw new DatabaseException(
						$"Failed to construct '{implementation.FullName}' for: {serviceInterfacesList}. " +
						$"Ensure it has a public constructor accepting '{nameof(ISqlServerDbContextFactory)}'.",
						innerException: ex,
						errorCode: "INVALID_CONFIGURATION");
				}

				foreach (var serviceInterface in group.OrderBy(t => t.FullName, StringComparer.Ordinal))
				{
					var registerMethod = registerOpenMethod.MakeGenericMethod(serviceInterface);
					registerMethod.Invoke(registry, new[] { instance });
				}
			}
		}

		/// <inheritdoc/>
		public void Shutdown()
		{
			DbContextFactory.Shutdown();
		}

		/// <inheritdoc/>
		public Task ShutdownAsync(CancellationToken cancellationToken = default)
		{
			return DbContextFactory.ShutdownAsync(cancellationToken);
		}
	}
}
