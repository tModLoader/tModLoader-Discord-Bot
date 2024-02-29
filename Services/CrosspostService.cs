using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace tModloaderDiscordBot.Services
{
	/// <summary>
	/// Automatically publishes all messages posted in 1․4․4-mod-porting-progress
	/// </summary>
	internal class CrosspostService : BaseService
	{
		internal ITextChannel crosspostChannel;
		private bool _isSetup = false;
#if TESTBOT
		private const ulong crosspostChannelId = 1159241301007536199; // #1․4․4-mod-porting-progress
#else
		private const ulong crosspostChannelId = 1111396440904831057;
#endif

		public CrosspostService(IServiceProvider services) : base(services)
		{
			var _client = services.GetRequiredService<DiscordSocketClient>();

			_client.MessageReceived += _client_MessageReceived;
		}

		private async Task _client_MessageReceived(SocketMessage socketMessage)
		{
			if (!await Setup())
				return;

			if (socketMessage?.Channel?.Id != crosspostChannelId)
				return;

			if (!(socketMessage is SocketUserMessage message))
				//|| message.Author.IsBot
				//|| message.Author.IsWebhook
				//|| !(message.Channel is SocketTextChannel channel))
				return;

			// await _loggingService.Log(new LogMessage(LogSeverity.Info, "Crosspost", $"Attempting to publish message {message.Id}."));
			await message.CrosspostAsync();
		}

		internal async Task<bool> Setup()
		{
			if (!_isSetup)
				_isSetup = await Task.Run(async () =>
				{
					crosspostChannel = (ITextChannel)_client.GetChannel(crosspostChannelId);
					return true;
				});
			return _isSetup;
		}
	}
}

