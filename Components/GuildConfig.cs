using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Components
{
	public sealed class GuildConfig
	{
		public ulong GuildId;
		public IList<SiteStatus> SiteStatuses = new List<SiteStatus>();
		public IList<GuildTag> GuildTags = new List<GuildTag>();
		public BotPermissions Permissions = new BotPermissions();

		[JsonIgnore] private GuildConfigService _guildConfigService;

		public void Initialize(GuildConfigService guildConfigService)
		{
			_guildConfigService = guildConfigService;
		}

		public GuildConfig(SocketGuild guild)
		{
			if (guild != null)
			{
				GuildId = guild.Id;
			}
		}

		public async Task<bool> Update()
		{
			if (_guildConfigService == null) 
				return false;

			await _guildConfigService.UpdateCacheForConfig(this);
			await _guildConfigService.WriteGuildConfig(this);
			return true;
		}
	}
}
