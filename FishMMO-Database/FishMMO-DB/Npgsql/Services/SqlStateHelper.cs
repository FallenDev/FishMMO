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
				if (current is SqlException sqlEx && sqlEx.IsTransient) return true;
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
