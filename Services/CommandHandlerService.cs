using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Services
{
	public class CommandHandlerService
	{
		private readonly CommandService _commandService;
		private readonly DiscordSocketClient _client;
		private readonly IServiceProvider _services;

		public CommandHandlerService(IServiceProvider services)
		{
			_commandService = services.GetRequiredService<CommandService>();
			_client = services.GetRequiredService<DiscordSocketClient>();
			_services = services;

			_client.MessageReceived += HandleCommand;
		}

		~CommandHandlerService()
		{
			_client.MessageReceived -= HandleCommand;
		}

		public async Task InitializeAsync()
		{
			await _commandService.AddModulesAsync(Assembly.GetEntryAssembly());
		}

		private async Task HandleCommand(SocketMessage socketMessage)
		{
			if (!Program.Ready) return;

			if (!(socketMessage is SocketUserMessage message)
				|| message.Author.IsBot
				|| message.Author.IsWebhook
				|| !(message.Channel is SocketTextChannel channel))
				return;

			var context = new SocketCommandContext(_client, message);

			int argPos = 0;
			if (message.Content.EqualsIgnoreCase(".")
				|| !(message.HasCharPrefix('.', ref argPos)))
				return;

			// todo handle tags

			var result = await _commandService.ExecuteAsync(context, argPos, _services);

			if (!result.IsSuccess)
			{
				if (!result.ErrorReason.EqualsIgnoreCase("Unknown command."))
					await context.Channel.SendMessageAsync(result.ErrorReason);
			}
		}
	}
}
