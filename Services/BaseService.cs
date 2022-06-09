using System;
using System.Diagnostics.CodeAnalysis;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace tModloaderDiscordBot.Services
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class BaseService : IBotService
    {
	    protected readonly DiscordSocketClient _client;
	    protected readonly LoggingService _loggingService;

		protected BaseService(IServiceProvider services)
	    {
			_client = services.GetRequiredService<DiscordSocketClient>();
			_loggingService = services.GetRequiredService<LoggingService>();
	    }
	}
}
