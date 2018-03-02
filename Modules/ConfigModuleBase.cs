using System;
using dtMLBot.Configs;
using Discord.Commands;

namespace dtMLBot.Modules
{
	// TODO probably only need Socket context
	public abstract class ConfigModuleBase<T> : ModuleBase<T> where T : class, ICommandContext
	{
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		public GuildConfig Config;
		public readonly CommandService CommandService;

		protected ConfigModuleBase(CommandService commandService)
		{
			CommandService = commandService;
		}

		// Note: Context is set before execute, not availalbe in constructor
		protected override void BeforeExecute(CommandInfo command)
		{
			base.BeforeExecute(command);

			Config = ConfigManager.GetManagedConfig(Context.Guild.Id);
			if (Config == null)
				throw new Exception("Failed to get guild config");
		}
	}
}
