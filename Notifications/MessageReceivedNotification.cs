using System;
using Discord.WebSocket;
using MediatR;

namespace tModloaderDiscordBot.Notifications;

/// <summary>
/// A message received event from Discord
/// </summary>
public class MessageReceivedNotification(SocketMessage message) : INotification
{
	public SocketMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));
}