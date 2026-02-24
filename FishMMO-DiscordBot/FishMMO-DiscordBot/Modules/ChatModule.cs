using Discord.Commands;
using FishMMO.Database.SqlServer; // Assuming SqlServerDbContext is here

namespace FishMMO.DiscordBot.Modules
{
	public class ChatModule : ModuleBase<SocketCommandContext>
	{
		private readonly SqlServerDbContext dbContext;

		public ChatModule(SqlServerDbContext dbContext)
		{
			this.dbContext = dbContext;
		}
	}
}