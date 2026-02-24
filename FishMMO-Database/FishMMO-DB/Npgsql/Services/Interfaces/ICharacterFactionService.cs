using FishMMO.Database.Data;
using FishMMO.Database.SqlServer.Services.Interfaces.Actions;

namespace FishMMO.Database.SqlServer.Services.Interfaces
{
	/// <summary>
	/// Service interface for managing character factions.
	/// </summary>
	/// <remarks>
	/// Faction persistence and deletion should be version-gated via the logical <c>Version</c>
	/// so stale updates are rejected and newer authoritative updates win.
	/// </remarks>
	public interface ICharacterFactionService :
		IPersistManyAction<CharacterFactionData>,
		IDeleteByKeyVersionedAction<long>,
		IFetchCollectionByKeyAction<long, CharacterFactionData>
	{
	}
}