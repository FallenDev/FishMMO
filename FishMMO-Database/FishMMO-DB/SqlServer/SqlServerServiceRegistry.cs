using System;
using System.Collections.Concurrent;
using System.Linq;
using FishMMO.Database.Exceptions;

namespace FishMMO.Database.SqlServer
{
	/// <summary>
	/// Service registry for SQL Server-backed database services.
	/// </summary>
	public sealed class SqlServerServiceRegistry : IDatabaseServiceRegistry
	{
		private readonly ConcurrentDictionary<Type, object> services = new ConcurrentDictionary<Type, object>();
		public int ServiceCount => services.Count;

		public bool TryGet<TService>(out TService service) where TService : class
		{
			if (services.TryGetValue(typeof(TService), out var serviceInstance))
			{
				service = (serviceInstance as TService)!;
				return service != null;
			}
			service = null!;
			return false;
		}

		public bool TryGet(Type serviceType, out object service)
		{
			if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
			return services.TryGetValue(serviceType, out service);
		}

		public bool IsRegistered<TService>() where TService : class => services.ContainsKey(typeof(TService));
		public bool IsRegistered(Type serviceType) => serviceType != null && services.ContainsKey(serviceType);
		public Type[] GetRegisteredServiceTypes() => services.Keys.ToArray();

		internal void Register<TService>(TService serviceInstance) where TService : class
		{
			if (serviceInstance == null) throw new ArgumentNullException(nameof(serviceInstance));
			if (!typeof(TService).IsInterface)
				throw new DatabaseException($"Services must be registered by interface type. Attempted '{typeof(TService).FullName}'.", errorCode: "INVALID_OPERATION");
			if (!services.TryAdd(typeof(TService), serviceInstance))
				throw new DatabaseException($"Service already registered: {typeof(TService).FullName}", errorCode: "INVALID_OPERATION");
		}
	}
}
