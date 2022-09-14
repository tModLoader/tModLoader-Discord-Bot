using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace tModloaderDiscordBot.Services
{
	internal class SupportChannelAutoMessageService : BaseService
	{
		internal ITextChannel supportChannel;
		private bool _isSetup = false;
#if TESTBOT
		private const ulong supportChannelId = ;
#else
		private const ulong supportChannelId = 871289059396448337;
#endif

		public SupportChannelAutoMessageService(IServiceProvider services) : base(services)
		{
			var _client = services.GetRequiredService<DiscordSocketClient>();

			_client.ThreadCreated += _client_ThreadCreated;
		}

		internal async Task<bool> Setup()
		{
			if (!_isSetup)
				_isSetup = await Task.Run(() =>
				{
					supportChannel = (ITextChannel)_client.GetChannel(supportChannelId);
					return true;
				});
			return _isSetup;
		}

		private async Task _client_ThreadCreated(SocketThreadChannel thread)
		{
			if (!await Setup())
				return;

			if (thread.ParentChannel != supportChannel)
				return;

			if (!thread.HasJoined) // called when created and when user is added, send message adds the bot, so this check is needed to prevent double posting.
			{
				await thread.SendMessageAsync($"Welcome to {supportChannel.Mention}. Before someone helps you, please first consult the pins in {supportChannel.Mention} and try all the suggestions that might fit your particular issue.\n\nIf the pins do not solve your issue, please post all log files by dragging and dropping them into this chat. In Steam right click on `tModLoader` in the library, then hover over `Manage` and click on `Browse local files`. In the folder that appears find `tModLoader-Logs` and open that folder. Inside that folder are the logs files. Select them all except the `Old` folder and drag them into this chat. If you need a visual guide to this process watch this: <https://gfycat.com/CarefreeVastFrillneckedlizard>");

				// TODO: use new ComponentBuilder().WithButton to spawn button to allow the user and any support staff to archive the thread to clean up the sidebar. (is archive the same as close? Will this remove it from all users sidebar?)
				//await thread.ModifyAsync(x => x.Archived = true);
			}
		}
	}
}
