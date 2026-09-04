using Discord;
using Discord.Commands;

namespace tModloaderDiscordBot.Factories
{
	internal class CommandServiceFactory
	{
		private CommandServiceFactory()
		{
			
		}

		public static CommandServiceConfig CreateCommandServiceConfig()
		{
			return new CommandServiceConfig
			{
				DefaultRunMode = RunMode.Async,
				CaseSensitiveCommands = false,
#if TESTBOT
				LogLevel = LogSeverity.Critical,
				ThrowOnError = true,
#else
				LogLevel = LogSeverity.Debug,
				ThrowOnError = false
#endif
			};
		}

		public static CommandService CreateCommandService()
		{
			return new CommandService(CreateCommandServiceConfig());
		}
	}
}
