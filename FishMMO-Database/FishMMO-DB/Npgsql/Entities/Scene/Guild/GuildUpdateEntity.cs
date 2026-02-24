using System;

namespace FishMMO.Database.SqlServer.Entities
{
	public class GuildUpdateEntity
	{
		public long ID { get; set; }
		public long GuildID { get; set; }
		public DateTime TimeCreated { get; set; }
		public DateTime LastUpdate { get; set; }
	}
}