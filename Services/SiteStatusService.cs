using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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

	public class SiteStatusService
	{
		private readonly IDictionary<ulong, IList<SiteStatus>> _siteStatuses;
		private readonly GuildConfigService _configService;
		private ulong _gid;

		// ReSharper disable once InconsistentNaming
		public void SetGID(ulong gid)
		{
			_gid = gid;
		}

		public bool HasName(string name) => _siteStatuses[_gid].Any(x => x.Name.EqualsIgnoreCase(name));
		public bool HasAddress(string addr) => _siteStatuses[_gid].Any(x => x.Address.EqualsIgnoreCase(addr));

		public IEnumerable<SiteStatus> AllSiteStatuses()
		{
			for (int i = 0; i < _siteStatuses[_gid].Count; i++)
			{
				yield return _siteStatuses[_gid][i];
			}
		}

		public (string cachedResult, string url) GetCachedResult(string key)
		{
			var status = _siteStatuses[_gid].FirstOrDefault(x => x.Name.EqualsIgnoreCase(key));
			if (status == null) return (null, null);
			return (status.CachedResult, status.Address);
		}

		public SiteStatusService(IServiceProvider services)
		{
			_configService = services.GetRequiredService<GuildConfigService>();
			_siteStatuses = new Dictionary<ulong, IList<SiteStatus>>();

			var _updateCacheTimer = new Timer((e) =>
			{
				foreach (var status in _siteStatuses.SelectMany(x => x.Value))
				{
					status.Revalidate();
				}
			}, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
		}

		public async Task UpdateAsync()
		{
			await Task.Run(async () =>
			{
				foreach (var config in _configService.GetAllConfigs())
				{
					await UpdateForConfig(config);
				}
			});
		}

		public async Task UpdateForConfig(GuildConfig config)
		{
			void AddOne(SiteStatus siteStatus)
			{
				siteStatus.Revalidate();
				_siteStatuses[config.GuildId].Add(siteStatus);
			}

			void RemoveOne(SiteStatus siteStatus)
			{
				_siteStatuses[config.GuildId].Remove(siteStatus);
			}

			await Task.Run(() =>
			{
				if (_siteStatuses.ContainsKey(config.GuildId))
				{
					foreach (var siteStatus in config.SiteStatuses.Except(_siteStatuses.SelectMany(x => x.Value)))
					{
						AddOne(siteStatus);
					}

					foreach (var siteStatus in _siteStatuses.SelectMany(x => x.Value).Except(config.SiteStatuses))
					{
						RemoveOne(siteStatus);
					}
				}
				else
				{
					_siteStatuses.Add(config.GuildId, new List<SiteStatus>());
					foreach (var siteStatus in config.SiteStatuses)
					{
						AddOne(siteStatus);
					}
				}
			});
		}

		public async Task RevalidateForGuild(ulong id)
		{
			await Task.Run(() =>
			{
				if (!_siteStatuses.ContainsKey(id)) return;

				foreach (var status in _siteStatuses[id])
				{
					status.Revalidate();
				}
			});
		}
	}
}
