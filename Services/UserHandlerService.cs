using System;
using System.Collections.Generic;

namespace tModloaderDiscordBot.Services
{
    public class UserHandlerService : GuildConfigService 
    {
		private readonly IDictionary<ulong, DateTime> _botCommandCooldowns = new Dictionary<ulong, DateTime>();

	    public UserHandlerService(IServiceProvider services) : base(services)
	    {
	    }

	    public bool UserMatchesPrerequisites(ulong id)
	    {
		    if (!UserHasBotCooldown(id))
			    return true;

		    DateTime cooldownTime = _botCommandCooldowns[id];

		    if (cooldownTime >= DateTime.Now)
			    return false;

		    _botCommandCooldowns.Remove(id);
		    return true;
	    }

	    public bool UserHasBotCooldown(ulong id) 
			=> _botCommandCooldowns.ContainsKey(id);

	    public void AddBotCooldown(ulong id, DateTime time) 
			=> _botCommandCooldowns.Add(id, time);

	    public void UpdateBotCooldown(ulong id, DateTime time)
	    {
		    if (!UserHasBotCooldown(id))
		    {
				AddBotCooldown(id, time);
		    }
		    else
		    {
			    _botCommandCooldowns[id] = time;
		    }
	    }

	    public void AddBasicBotCooldown(ulong id)
	    {
			UpdateBotCooldown(id, DateTime.Now.AddSeconds(3));
	    }
    }
}
