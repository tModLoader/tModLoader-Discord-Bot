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
	class HastebinService
	{
		private static readonly Regex _HasteKeyRegex = new Regex(@"{""key"":""(?<key>[a-z].*)""}", RegexOptions.Compiled);

		private readonly DiscordSocketClient _client;
		private readonly LoggingService _loggingService;

		public HastebinService(IServiceProvider services)
		{
			_loggingService = services.GetRequiredService<LoggingService>();
			_client = services.GetRequiredService<DiscordSocketClient>();

			_client.MessageReceived += HandleCommand;
		}

		~HastebinService()
		{
			_client.MessageReceived -= HandleCommand;
		}

		private async Task HandleCommand(SocketMessage socketMessage)
		{
			// Program is ready
			if (!Program.Ready) return;

			// Valid message, no bot, no webhook, and valid channel
			if (!(socketMessage is SocketUserMessage message)
				|| message.Author.IsBot
				|| message.Author.IsWebhook
				|| !(message.Channel is SocketTextChannel channel))
				return;

			var context = new SocketCommandContext(_client, message);

			if (string.IsNullOrWhiteSpace(message.Content))
				return;

			int count = 0;
			foreach (char c in message.Content)
			{
				if (c == '{') count++;
				if (c == '}') count++;
				if (c == '=') count++; 
				if (c == ';') count++;
			}

			if(count > 1 && message.Content.Split('\n').Length > 8)
			{
				string hastebinContent = message.Content;
				hastebinContent = hastebinContent.Trim('`');

				//var msg = await context.Channel.SendMessageAsync("Auto Hastebin in progress");
				using (var client = new HttpClient())
				{
					HttpContent content = new StringContent(hastebinContent);

					var response = await client.PostAsync("https://hastebin.com/documents", content);
					string resultContent = await response.Content.ReadAsStringAsync();

					var match = _HasteKeyRegex.Match(resultContent);

					if (!match.Success)
					{
						// hastebin down?
						return;
					}

					string hasteUrl = $"https://hastebin.com/{match.Groups["key"]}.cs";
					await context.Channel.SendMessageAsync($"Automatic Hastebin for {message.Author.Username}: {hasteUrl}");
					await message.DeleteAsync();
				}
			}
		}
	}
}
