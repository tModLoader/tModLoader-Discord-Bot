using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Notifications;

namespace tModloaderDiscordBot;

/// <summary>
/// Listens to Discord's events and creates notifications for them commands and forwards it to Mediatr
/// </summary>
public class DiscordEventListener(DiscordSocketClient client, IServiceScopeFactory serviceScope)
{
	private readonly CancellationToken _cancellationToken = new CancellationTokenSource().Token;
	
	private IMediator Mediator
	{
		get
		{
			var scope = serviceScope.CreateScope();
			return scope.ServiceProvider.GetRequiredService<IMediator>();
		}
	}
	
	public async Task SetupAsync()
	{
		client.MessageReceived += OnMessageReceivedAsync;
		await Task.CompletedTask;
	}

	private Task OnMessageReceivedAsync(SocketMessage arg)
	{
		return Mediator.Publish(new MessageReceivedNotification(arg), _cancellationToken);
	}
}