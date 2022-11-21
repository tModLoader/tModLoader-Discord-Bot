using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Services
{
	public class SiteStatusService : BaseConfigService
	{
		public bool HasName(string name)
			=> _guildConfig.SiteStatuses.Any(x => x.Name.EqualsIgnoreCase(name));
		public bool HasAddress(string addr)
			=> _guildConfig.SiteStatuses.Any(x => x.Address.EqualsIgnoreCase(addr));

		public IEnumerable<SiteStatus> AllSiteStatuses()
		{
			for (int i = 0; i < _guildConfig.SiteStatuses.Count; i++)
				yield return _guildConfig.SiteStatuses[i];
		}

		public (string cachedResult, string url) GetCachedResult(string key)
		{
			var status = _guildConfig.SiteStatuses.FirstOrDefault(x => x.Name.EqualsIgnoreCase(key));
			return status == null ? (null, null) : (status.CachedResult, status.Address);
		}

		private Timer _updateCacheTimer;

		public SiteStatusService(IServiceProvider services) : base(services)
		{
			_updateCacheTimer = new Timer((e) =>
			{
				Task.Run(async () =>
				{
					foreach (var config in _guildConfigService.GetAllConfigs().ToList())
						foreach (var status in config.SiteStatuses)
							await status.Revalidate();
				});	
			}, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
		}

		public async Task UpdateAsync()
		{
			foreach (var config in _guildConfigService.GetAllConfigs())
				await UpdateForConfig(config);
		}

		public async Task UpdateForConfig(GuildConfig config)
		{
			await Task.WhenAll(
				config.SiteStatuses.Where(x => x.StatusCode == SiteStatusCode.Unknown)
				.ToAsyncEnumerable()
				.ForEachAwaitAsync(async x => await x.Revalidate())
			);
		}
	}
}
