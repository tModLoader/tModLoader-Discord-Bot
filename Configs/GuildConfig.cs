using Discord.WebSocket;

namespace tModloaderDiscordBot.Configs
{
	public sealed class GuildConfig
	{
		public ulong GuildId;

		public GuildConfig(SocketGuild guild)
		{
			if (guild != null)
			{
				GuildId = guild.Id;
			}
		}
	}
}
