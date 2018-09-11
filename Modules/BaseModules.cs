using System;
using Discord.Commands;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Modules
{
	public abstract class BotModuleBase : ModuleBase<SocketCommandContext>
	{
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		public CommandService CommandService { get; set; }
	}

	public abstract class ConfigModuleBase : BotModuleBase
	{
		public GuildConfigService GuildConfigService { get; set; }
		[DontInject] public GuildConfig Config { get; set; }

		// Note: Context is set before execute, not available in constructor
		protected override void BeforeExecute(CommandInfo command)
		{
			base.BeforeExecute(command);

			if (GuildConfigService == null)
				throw new Exception("Failed to get guild config service");

			Config = GuildConfigService.GetConfig(Context.Guild.Id);
			if (Config == null)
				throw new Exception("Failed to get guild config");

			Config.Initialize(GuildConfigService);
		}
	}
}
