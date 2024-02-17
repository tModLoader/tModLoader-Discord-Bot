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
		_client.ThreadCreated += ThreadCreated;
	}

	private async Task ThreadCreated(SocketThreadChannel arg)
	{
		// Must be a thread
		if (arg is not SocketThreadChannel thread)
			return;

		// In a forum channel
		if (thread.ParentChannel is not SocketForumChannel forum)
			return;

		var messages = await thread.GetMessagesAsync().FlattenAsync();
		if (messages.FirstOrDefault() is not IUserMessage firstMessage)
			return;

		if (firstMessage.IsPinned)
			return;

		await firstMessage.PinAsync();
	}
}
