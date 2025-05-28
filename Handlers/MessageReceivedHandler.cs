using System.Threading;
using System.Threading.Tasks;
using MediatR;
using tModloaderDiscordBot.Notifications;

namespace tModloaderDiscordBot.Handlers;

public class MessageReceivedHandler : INotificationHandler<MessageReceivedNotification>
{
	public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
	{
		var message = notification.Message;
		return Task.CompletedTask;
	}
}