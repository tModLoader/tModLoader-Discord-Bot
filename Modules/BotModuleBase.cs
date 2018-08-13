using System;
using Discord.Commands;
using tModloaderDiscordBot.Configs;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Modules
{
	public abstract class BotModuleBase : ModuleBase<SocketCommandContext>
	{
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		protected GuildConfig Config;
		public CommandService CommandService { get; set; }
		public GuildConfigService GuildConfigService { get; set; }

		//protected BotModuleBase(CommandService commandService, GuildConfigService guildConfigService)
		//{
		//	CommandService = commandService;
		//	GuildConfigService = guildConfigService;
		//}

		// Note: Context is set before execute, not availalbe in constructor
		protected override void BeforeExecute(CommandInfo command)
		{
			base.BeforeExecute(command);

			if (GuildConfigService == null)
				throw new Exception("Failed to get guild config service");

			Config = GuildConfigService.GetGuildConfig(Context.Guild.Id);
			if (Config == null)
				throw new Exception("Failed to get guild config");
		}
	}
}
