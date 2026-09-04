using Discord.Interactions;
using tModloaderDiscordBot.Modules;
using tModloaderDiscordBot.Notifications;

namespace tModloaderDiscordBot;

public static class SocketInteractionContextExtensions
{
	public static ModCommandNotification ToModCommand(
		this SocketInteractionContext ctx,
		InteractionModule interactionModule,
		string mod
	)
	{
		return new ModCommandNotification(ctx, interactionModule, mod);
	}
}