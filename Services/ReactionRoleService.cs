/* WIP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Services
{
	class ReactionRoleService
	{
		private readonly DiscordSocketClient _client;
		private readonly LoggingService _loggingService;

		public ReactionRoleService(IServiceProvider services)
		{
			_loggingService = services.GetRequiredService<LoggingService>();
			_client = services.GetRequiredService<DiscordSocketClient>();

			_client.ReactionAdded += HandleReactionAdded;
			_client.ReactionRemoved += HandleReactionRemoved;
		}

		private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
		{
			if (arg1.Id == 556639202591375374 && arg3.User.Value is SocketGuildUser user && arg2 is SocketGuildChannel messageChannel)
			{
				await _loggingService.Log(new LogMessage(LogSeverity.Info, "ReactionRole", $"Reaction added"));
				string emoteName = arg3.Emote.Name;
				if (emoteName != "🎉" && emoteName != "💟")
					return;
				string roleName = emoteName == "🎉" ? "testrole" : "testrole2";
				var role = messageChannel.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
				if (role != null)
					await user.AddRoleAsync(role);
			}
		}

		private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
		{
			if (arg1.Id == 556639202591375374 && arg3.User.Value is SocketGuildUser user && arg2 is SocketGuildChannel messageChannel)
			{
				await _loggingService.Log(new LogMessage(LogSeverity.Info, "ReactionRole", $"Reaction removed"));
				string emoteName = arg3.Emote.Name;
				if (emoteName != "🎉" && emoteName != "💟")
					return;
				string roleName = emoteName == "🎉" ? "testrole" : "testrole2";
				var role = messageChannel.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
				if(role != null)
					await user.RemoveRoleAsync(role);
			}
		}

		IMessageChannel reactionChannel;
		IMessage reactionMessage;

		internal async Task Maintain(DiscordSocketClient client)
		{
			var channel = _client.GetChannel(556634834093473793);
			if (channel is IMessageChannel reactionChannel)
			{
				var message = await reactionChannel.GetMessageAsync(556639202591375374);
				if(message is IUserMessage reactionMessage)
				{
					await _loggingService.Log(new LogMessage(LogSeverity.Info, "ReactionRole", $"Reaction message found"));
				}
			}
		}
	}
}
*/