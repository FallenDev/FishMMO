namespace FishMMO.Database.SqlServer.Services
{
	internal static class SqlServerSqlState
	{
		public const string UniqueViolation = "2627";
		public const string ForeignKeyViolation = "547";
		public const string NotNullViolation = "515";
		public const string CheckViolation = "547";
		public const string TooManyConnections = "10928";
		public const string ConnectionClassPrefix = "08";
		public const string AdminShutdown = "6005";
		public const string CrashShutdown = "6006";
		public const string CannotConnectNow = "40613";
		public const string ProtocolViolation = "233";
		public const string InternalError = "50000";
		public const string InvalidAuthorizationSpecification = "18456";
		public const string DeadlockDetected = "1205";
		public const string SerializationFailure = "3960";
		public const string LockNotAvailable = "1222";
		public const string QueryCanceled = "-2";
	}
}
