using Discord.Interactions;
using MediatR;
using tModloaderDiscordBot.Modules;

namespace tModloaderDiscordBot.Notifications;

public interface IInteractionNotification : INotification
{
	public SocketInteractionContext Context { get; }
	public InteractionModule InteractionModule { get; }
}