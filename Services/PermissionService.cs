using System;
using tModloaderDiscordBot.Components;


namespace tModloaderDiscordBot.Services
{
    public class PermissionService : BaseConfigService
    {
	    public PermissionService(IServiceProvider services) : base(services)
	    {
		}

	    public BotPermissions GetGuildPermissions()
	    {
		    return _guildConfig.Permissions;
	    }
    }
}
