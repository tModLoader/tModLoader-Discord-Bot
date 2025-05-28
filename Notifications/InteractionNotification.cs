using System.Threading;
using Discord.Interactions;
using tModloaderDiscordBot.Modules;

namespace tModloaderDiscordBot.Notifications;

public class InteractionNotification(
	SocketInteractionContext context,
	InteractionModule interactionModule
) : IInteractionNotification
{
	public SocketInteractionContext Context { get; } = context;
	public InteractionModule InteractionModule { get; } = interactionModule;
}