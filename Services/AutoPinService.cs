using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace tModloaderDiscordBot.Services;

/// <summary>
/// Automatically pins each newly created forum post's opening message.
/// </summary>
public class AutoPinService : BaseService
{
	public AutoPinService(IServiceProvider services) : base(services)
	{
		_client.ChannelCreated += ChannelCreated;
	}

	private async Task ChannelCreated(SocketChannel arg)
	{
		// Must be a thread
		if (arg is not SocketThreadChannel thread)
			return;

		// In a forum channel
		if (thread.ParentChannel is not SocketForumChannel forum)
			return;

		if ((await thread.GetMessagesAsync().FirstAsync(m => m is IUserMessage)) is not IUserMessage firstMessage)
			return;

		if (firstMessage.IsPinned)
			return;

		await firstMessage.PinAsync();
	}
}
