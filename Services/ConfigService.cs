using System.Collections.Generic;
using tModloaderDiscordBot.Configs;

namespace tModloaderDiscordBot.Services
{
    public sealed class ConfigService
    {
		public IEnumerable<GuildConfig> GuildConfigs { get; }
    }
}
