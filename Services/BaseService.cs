using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace tModloaderDiscordBot.Services
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class BaseService : IBotService
    {
	    protected readonly LoggingService _loggingService;

	    protected BaseService(IServiceProvider services)
	    {
		    _loggingService = services.GetRequiredService<LoggingService>();
	    }
	}
}
