
namespace FishMMO.Database.SqlServer.Entities
{
	public interface IVersionedEntity
	{
		long ID { get; set; }
		long Version { get; set; }
	}
}