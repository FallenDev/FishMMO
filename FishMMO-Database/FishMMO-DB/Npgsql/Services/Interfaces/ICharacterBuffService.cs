using FishMMO.Database.Data;
using FishMMO.Database.SqlServer.Services.Interfaces.Actions;

namespace FishMMO.Database.SqlServer.Services.Interfaces
{
	/// <summary>
	/// Service interface for managing character buffs.
	/// </summary>
	/// <remarks>
	/// Buff persistence and deletion should be version-gated via the logical <c>Version</c>
	/// so stale updates are rejected and newer authoritative updates win.
	/// </remarks>
	public interface ICharacterBuffService :
		IPersistManyAction<CharacterBuffData>,
		IDeleteByKeyVersionedAction<long>,
		IFetchCollectionByKeyAction<long, CharacterBuffData>
	{
	}
}