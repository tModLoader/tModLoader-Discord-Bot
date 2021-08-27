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

		private readonly string[] CodeBlockTypes = new string[]
		{
			"html",
			"css",
			"cs",
			"dns",
			"python",
			"lua",
			"http",
			"markdown",
		};

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

		// TODO: Autohastebin .cs or .txt file attachments.
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

			string contents = message.Content;
			bool shouldHastebin = false;
			bool autoDeleteUserMessage = false;
			string extra = "";

			var attachents = message.Attachments;
			if(attachents.Count == 1 && attachents.ElementAt(0) is Attachment attachment)
			{
				if ((attachment.Filename.EndsWith(".log") || attachment.Filename.EndsWith(".cs") || attachment.Filename.EndsWith(".json") || attachment.Filename == "message.txt") && attachment.Size < 100000)
				{
					using (var client = new HttpClient())
						contents = await client.GetStringAsync(attachment.Url);

					shouldHastebin = true;
					extra = $" `({attachment.Filename})`";
				}
			}

			if (string.IsNullOrWhiteSpace(contents))
				return;

			int count = 0;
			if (!shouldHastebin)
			{
				foreach (char c in contents)
				{
					if (c == '{') count++;
					if (c == '}') count++;
					if (c == '=') count++;
					if (c == ';') count++;
				}
				if (count > 1 && message.Content.Split('\n').Length > 16)
				{
					shouldHastebin = true;
					autoDeleteUserMessage = true;
				}
			}

			if(shouldHastebin)
			{
				string hastebinContent = contents.Trim('`');
				for (int i = 0; i < CodeBlockTypes.Length; i++)
				{
					string keyword = CodeBlockTypes[i];
					if (hastebinContent.StartsWith(keyword + "\n"))
					{
						hastebinContent = hastebinContent.Substring(keyword.Length).TrimStart('\n');
						break;
					}
				}

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
					await context.Channel.SendMessageAsync($"Automatic Hastebin for {message.Author.Username}{extra}: {hasteUrl}");
					if(autoDeleteUserMessage)
						await message.DeleteAsync();
				}
			}
		}
	}
}
