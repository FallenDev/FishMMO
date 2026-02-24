namespace FishMMO.Database.SqlServer.Entities
{
	/// <summary>
	/// Represents a scalar integer result returned from raw SQL queries.
	/// </summary>
	/// <remarks>
	/// This is configured as a keyless entity type in <see cref="SqlServerDbContext"/> and is intended
	/// for mapping single-row results such as counts and status codes.
	/// </remarks>
	public sealed class SqlIntValue
	{
		/// <summary>
		/// Gets or sets the scalar integer value.
		/// </summary>
		public int Value { get; set; }
	}
}