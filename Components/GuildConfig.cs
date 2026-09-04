using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Components
{
	/// <summary>
	/// Defines the stored configuration of a guild
	/// </summary>
	public sealed class GuildConfig
	{
		/// <summary>
		/// The guild's snowflake Id this configuration belongs to
		/// </summary>
		public ulong GuildId;

		/// <summary>
		/// A list of site statuses being tracked by this configuration
		/// </summary>
		public IList<SiteStatus> SiteStatuses = [];

		/// <summary>
		/// The tags that belong to this configuration
		/// </summary>
		public IList<GuildTag> GuildTags = [];

		/// <summary>
		/// The bot's permissions
		/// </summary>
		public BotPermissions Permissions = new();

		[JsonIgnore] private GuildConfigService _guildConfigService;

		/// <summary>
		/// Initializes the configuration
		/// </summary>
		public void Initialize(GuildConfigService guildConfigService)
		{
			_guildConfigService = guildConfigService;
		}

		/// <summary>
		/// Creates a new configuration instance for the given guild
		/// </summary>
		public GuildConfig(SocketGuild guild)
		{
			if (guild != null)
			{
				GuildId = guild.Id;
			}
		}

		/// <summary>
		/// Attempts to update this configuration and write it to storage
		/// </summary>
		/// <returns></returns>
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
