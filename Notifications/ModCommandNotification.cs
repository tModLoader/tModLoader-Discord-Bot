using System;
using System.Threading;
using Discord.Interactions;
using Discord.WebSocket;
using tModloaderDiscordBot.Modules;

namespace tModloaderDiscordBot.Notifications;

/// <summary>
/// A mod command being executed
/// </summary>
public class ModCommandNotification(
	SocketInteractionContext ctx,
	InteractionModule interactionModule,
	string mod
) : InteractionNotification(ctx, interactionModule)
{
	public string ModName { get; } = mod;

	public ISocketMessageChannel Channel { get; } = ctx.Channel ?? throw new ArgumentNullException(nameof(Channel));
}