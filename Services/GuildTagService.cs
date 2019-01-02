using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tModloaderDiscordBot.Components;

namespace tModloaderDiscordBot.Services
{
	public class GuildTagService : BaseConfigService
	{
		public GuildTagService(IServiceProvider services) : base(services)
		{
		}

		public IEnumerable<GuildTag> GuildTags => _guildConfig?.GuildTags ?? Enumerable.Empty<GuildTag>();

		private IEnumerable<GuildTag> GuildTagsOwnedBy(ulong id) => GuildTags.Where(x => x.IsOwner(id));
		public bool HasTag(string key) => _guildConfig.GuildTags.Any(x => x.MatchesName(key));
		public bool HasTag(ulong id, string key) => GuildTagsOwnedBy(id).Any(x => x.MatchesName(key));
		public GuildTag GetTag(ulong id, string key) => GuildTagsOwnedBy(id).FirstOrDefault(x => x.MatchesName(key));
		public IEnumerable<GuildTag> GetTags(ulong id) => GuildTagsOwnedBy(id);
		public IEnumerable<GuildTag> GetTags(string predicate, ulong? id = null, bool globalTagsOnly = false)
		{
			var tags = id.HasValue ? GuildTagsOwnedBy(id.Value) : _guildConfig.GuildTags;

			foreach (var guildTag in tags)
			{
				if (globalTagsOnly && !guildTag.IsGlobal) continue;

				if (guildTag.MatchesName(predicate))
				{
					yield return guildTag;
					yield break;
				}
			}

			foreach (var guildTag in tags)
			{
				if (globalTagsOnly && !guildTag.IsGlobal) continue;

				if (guildTag.Name.Contains(predicate))
					yield return guildTag;
			}
		}

		public async Task AddNewTag(ulong id, string name, string value)
		{
			_guildConfig.GuildTags.Add(new GuildTag
			{
				IsGlobal = false,
				Name = name,
				OwnerId = id,
				Value = value
			});
			await RequestConfigUpdate();
		}

		public async Task RemoveTag(GuildTag guildTag)
		{
			_guildConfig.GuildTags.Remove(guildTag);
			await RequestConfigUpdate();
		}
	}
}
