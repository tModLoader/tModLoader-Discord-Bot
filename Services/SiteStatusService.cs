using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using tModloaderDiscordBot.Configs;

namespace tModloaderDiscordBot.Services
{
	public enum SiteStatusCode
	{
		Offline,
		Online,
		Unknown,
		Invalid
	}

	public class SiteStatus
	{
		internal static IDictionary<SiteStatusCode, string> StatusCodes = new Dictionary<SiteStatusCode, string>
		{
			{ SiteStatusCode.Invalid, "Invalid address" },
			{ SiteStatusCode.Online, "Online (Response OK)" },
			{ SiteStatusCode.Offline, "Offline (Response OK)" },
		};

		public string Name;
		public string Address;
		[JsonIgnore] public SiteStatusCode StatusCode = SiteStatusCode.Unknown;
		[JsonIgnore] public string CachedResult;

		public static bool IsValidEntry(ref string addr)
		{
			CheckUriPrefix(ref addr);
			return IsUriLegit(addr, out var _);
		}

		public static bool IsUriLegit(string addr, out Uri uri)
		{
			return Uri.TryCreate(addr, UriKind.Absolute, out uri)
				   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
		}

		public static void CheckUriPrefix(ref string addr)
		{
			if (addr.StartsWith("www."))
				addr = addr.Substring(3);

			if (!addr.StartsWith("http://") && !addr.StartsWith("https://"))
				addr = $"http://{addr}";
		}

		public void Revalidate()
		{
			bool result = IsValidEntry(ref Address);

			if (!result) StatusCode = SiteStatusCode.Invalid;
			try
			{
				StatusCode = (SiteStatusCode)Convert.ToInt32(Ping());
			}
			catch (Exception)
			{
				StatusCode = SiteStatusCode.Invalid;
			}

			CachedResult = StatusCodes[StatusCode];
		}

		internal bool Ping()
		{
			var request = WebRequest.Create(Address);
			return request.GetResponse() is HttpWebResponse response && response.StatusCode == HttpStatusCode.OK;
		}
	}

	public class SiteStatusService : BaseConfigService
	{
		public bool HasName(string name) => _guildConfig.SiteStatuses.Any(x => x.Name.EqualsIgnoreCase(name));
		public bool HasAddress(string addr) => _guildConfig.SiteStatuses.Any(x => x.Address.EqualsIgnoreCase(addr));

		public IEnumerable<SiteStatus> AllSiteStatuses()
		{
			for (int i = 0; i < _guildConfig.SiteStatuses.Count; i++)
			{
				yield return _guildConfig.SiteStatuses[i];
			}
		}

		public (string cachedResult, string url) GetCachedResult(string key)
		{
			var status = _guildConfig.SiteStatuses.FirstOrDefault(x => x.Name.EqualsIgnoreCase(key));
			return status == null ? (null, null) : (status.CachedResult, status.Address);
		}

		public SiteStatusService(IServiceProvider services) : base(services)
		{

			var _updateCacheTimer = new Timer((e) =>
			{
				foreach (var status in _guildConfig.SiteStatuses)
				{
					status.Revalidate();
				}
			}, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
		}

		public async Task UpdateAsync()
		{
			foreach (var config in _guildConfigService.GetAllConfigs())
			{
				await UpdateForConfig(config);
			}
		}

		public Task UpdateForConfig(GuildConfig config)
		{
			var needsValidation = config.SiteStatuses.Where(x => x.StatusCode == SiteStatusCode.Unknown);
			foreach (var siteStatus in needsValidation)
			{
				siteStatus.Revalidate();
			}
			return Task.CompletedTask;
		}
	}
}
