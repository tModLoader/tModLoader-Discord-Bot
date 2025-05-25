using System;
using Discord.Commands;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Modules
{
	/// <summary>
	/// Defines the base starting point for any command
	/// </summary>
	public abstract class BotModuleBase : ModuleBase<SocketCommandContext>
	{
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		public CommandService CommandService { get; set; }
	}
}
