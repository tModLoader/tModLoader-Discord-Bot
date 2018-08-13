using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Configs;

namespace tModloaderDiscordBot.Services
{
    public abstract class BaseConfigService
    {
	    protected readonly GuildConfigService _guildConfigService;
	    protected GuildConfig _guildConfig;
	    protected ulong _gid;

	    protected BaseConfigService(IServiceProvider services)
	    {
			_guildConfigService = services.GetRequiredService<GuildConfigService>();
		}

	    public virtual void Initialize(ulong gid)
	    {
		    _gid = gid;
		    _guildConfig = _guildConfigService.GetConfig(gid);
		}

	    public async Task RequestConfigUpdate()
	    {
		    await _guildConfig.Update(_guildConfigService);
	    }
	}
}
