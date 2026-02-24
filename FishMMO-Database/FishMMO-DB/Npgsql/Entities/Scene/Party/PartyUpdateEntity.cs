using System;

namespace FishMMO.Database.SqlServer.Entities
{
	public class PartyUpdateEntity
	{
		public long ID { get; set; }
		public long PartyID { get; set; }
		public DateTime TimeCreated { get; set; }
		public DateTime LastUpdate { get; set; }
	}
}