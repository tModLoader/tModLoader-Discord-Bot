using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MediatR;
using tModloaderDiscordBot.Modules;
using tModloaderDiscordBot.Notifications;

namespace tModloaderDiscordBot.Handlers;

/// <summary>
/// A base class that can be used for command handlers.
/// This base class already unpacks some useful variables, such as
/// the Context property, which can be used to reply using the interaction.
/// </summary>
public abstract class BaseCommandHandler<T> : INotificationHandler<T> where T : IInteractionNotification
{
	/// <summary>
	/// The command (notification) that was ran)
	/// </summary>
	protected T Command { get; private set; }

	protected CancellationToken CancellationToken { get; private set; }
	
	/// <summary>
	/// The context of the interaction
	/// </summary>
	protected SocketInteractionContext Context { get; private set; }

	/// <summary>
	/// The interaction, which can be replied to
	/// </summary>
	protected SocketInteraction Interaction { get; private set; }

	/// <summary>
	/// The interaction module this command was ran in
	/// </summary>
	protected InteractionModule  InteractionModule { get; private set; }

	/// <summary>
	/// Use this to run the command
	/// </summary>
	public Task Handle(T notification, CancellationToken cancellationToken)
	{
		CancellationToken = cancellationToken;
		Context = notification.Context;
		InteractionModule = notification.InteractionModule;
		Command = notification;
		Interaction = notification.InteractionModule.Context.Interaction;
		if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;
		return Task.Run(() => RunCommand(notification), cancellationToken);
	}

	/// <summary>
	/// Runs the command
	/// </summary>
	protected abstract Task RunCommand(T notification);
}