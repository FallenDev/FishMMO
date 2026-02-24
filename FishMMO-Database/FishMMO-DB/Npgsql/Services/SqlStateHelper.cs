using System;
using Microsoft.Data.SqlClient;

namespace FishMMO.Database.SqlServer.Services
{
	internal static class SqlStateHelper
	{
		public static string? TryGetSqlServerSqlState(Exception exception)
		{
			for (var current = exception; current != null; current = current.InnerException)
			{
				if (current is SqlException sqlEx)
					return sqlEx.Number.ToString();
			}
			return null;
		}

		public static bool IsTransientDatabaseFailure(Exception exception, string? sqlState)
		{
			if (exception is OperationCanceledException) return false;

			for (var current = exception; current != null; current = current.InnerException)
			{
				if (current is SqlException sqlEx && IsTransientSqlErrorNumber(sqlEx.Number)) return true;
			}

			if (exception is TimeoutException) return true;

			if (!string.IsNullOrWhiteSpace(sqlState))
			{
				return IsTimeoutSqlState(sqlState)
					|| IsConnectionSqlState(sqlState)
					|| IsTransientSqlState(sqlState)
					|| IsPgBouncerTransientSqlState(sqlState);
			}

			return false;
		}

		private static bool IsTransientSqlErrorNumber(int number)
		{
			return number == -2 // Timeout
				|| number == 20 // Login failure / encryption / network-related
				|| number == 64 // Connection dropped
				|| number == 233 // Initialization / login process failure
				|| number == 1205 // Deadlock victim
				|| number == 4060 // Cannot open requested database
				|| number == 40197 // Service encountered an error and closed connection
				|| number == 40501 // Service busy / throttling
				|| number == 40613 // Database unavailable
				|| number == 49918 // Cannot process request now
				|| number == 49919 // Too many create/update operations in progress
				|| number == 49920; // Too many operations in progress
		}

		public static bool IsTimeoutSqlState(string? sqlState) => string.Equals(sqlState, SqlServerSqlState.QueryCanceled, StringComparison.Ordinal);

		public static bool IsConnectionSqlState(string? sqlState)
		{
			if (string.IsNullOrWhiteSpace(sqlState)) return false;
			return sqlState.StartsWith(SqlServerSqlState.ConnectionClassPrefix, StringComparison.Ordinal)
				|| sqlState == SqlServerSqlState.AdminShutdown
				|| sqlState == SqlServerSqlState.CrashShutdown
				|| sqlState == SqlServerSqlState.CannotConnectNow;
		}

		public static bool IsPgBouncerTransientSqlState(string? sqlState)
		{
			if (string.IsNullOrWhiteSpace(sqlState)) return false;
			return sqlState == SqlServerSqlState.ProtocolViolation
				|| sqlState == SqlServerSqlState.AdminShutdown
				|| sqlState == SqlServerSqlState.InternalError;
		}

		public static bool IsPgBouncerConfigurationSqlState(string? sqlState) =>
			string.Equals(sqlState, SqlServerSqlState.InvalidAuthorizationSpecification, StringComparison.Ordinal);

		public static bool IsTransientSqlState(string? sqlState)
		{
			if (string.IsNullOrWhiteSpace(sqlState)) return false;
			return sqlState == SqlServerSqlState.DeadlockDetected
				|| sqlState == SqlServerSqlState.SerializationFailure
				|| sqlState == SqlServerSqlState.LockNotAvailable
				|| sqlState == SqlServerSqlState.TooManyConnections
				|| sqlState == SqlServerSqlState.InternalError;
		}
	}
}
