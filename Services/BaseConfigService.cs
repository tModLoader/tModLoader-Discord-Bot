using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Components;

namespace tModloaderDiscordBot.Services
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class BaseConfigService : IBotService
    {
	    protected readonly LoggingService _loggingService;
	    protected readonly GuildConfigService _guildConfigService;
	    protected GuildConfig _guildConfig;
	    protected ulong _gid;

	    protected BaseConfigService(IServiceProvider services)
	    {
		    _loggingService = services.GetRequiredService<LoggingService>();
			_guildConfigService = services.GetRequiredService<GuildConfigService>();
		}

	    public virtual void Initialize(ulong gid)
	    {
		    _gid = gid;
		    _guildConfig = _guildConfigService.GetConfig(gid);
			_guildConfig.Initialize(_guildConfigService);
		}

	    public async Task RequestConfigUpdate()
	    {
		    await _guildConfig.Update();
	    }
	}
}
