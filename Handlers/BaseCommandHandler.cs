using System.Threading;
using System.Threading.Tasks;
using Discord.Interactions;
using MediatR;
using tModloaderDiscordBot.Modules;
using tModloaderDiscordBot.Notifications;

namespace tModloaderDiscordBot.Handlers;

/// <summary>
/// A base class that can be used for command handlers.
/// Use the unpack method to unpack any notification information
/// into local variables or properties. This base class already contains the
/// IInteractionModuleBase which the command originated from.
/// </summary>
public abstract class BaseCommandHandler<T> : InteractionModuleBase, INotificationHandler<T> where T : IInteractionNotification
{
	protected T Command { get; private set; }

	protected CancellationToken CancellationToken { get; private set; }
	
	protected SocketInteractionContext  Context { get; private set; }
	
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
		return Task.Run(() => RunCommand(notification), cancellationToken);
	}

	protected abstract Task RunCommand(T notification);
}