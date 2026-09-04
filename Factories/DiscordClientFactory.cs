using Discord;
using Discord.WebSocket;

namespace tModloaderDiscordBot.Factories
{
	internal class DiscordClientFactory
	{
		private DiscordClientFactory() { }

		public static DiscordSocketConfig CreateDiscordSocketConfig()
		{
			return new DiscordSocketConfig
			{
				GatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers) & ~(GatewayIntents.GuildScheduledEvents | GatewayIntents.GuildInvites),
				AlwaysDownloadUsers = true,
				LogLevel = LogSeverity.Verbose,
				MessageCacheSize = 100
			};
		}

		public static IDiscordClient CreateDiscordSocketClient()
		{
			return new DiscordSocketClient(CreateDiscordSocketConfig());
		}
	}
}
