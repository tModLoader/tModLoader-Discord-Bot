using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;

namespace tModloaderDiscordBot.Configs
{
	public sealed class GuildConfig : ICloneable, IEquatable<GuildConfig>
	{
		public ulong GuildId;
		public IDictionary<ulong, IList<ulong>> StickyRoles; // roles that are stickied -> reapplies when leaving and rejoining
		public IDictionary<string, string> StatusAddresses; // addresses that can be checked with .status
		[JsonIgnore] public IDictionary<string, string> StatusAddressesCache; // statuse cached every 5 mins
		public ISet<ulong> VoteDeleteImmune; // IDs immune to vote deletion
		public IDictionary<ulong, IList<KeyValTag>> Tags; // stored tags, uid -> set of tags
		public IDictionary<ulong, uint> UserRateLimitCounts; // uid -> number of rate limits
		public ISet<ulong> VoteDeleteReqIds; // req ids (role/usr) for vote deletion
		public IDictionary<ulong, uint> VoteDeleteWeights; // id -> vote weight (admin=3 etc.)
		public uint VoteDeleteReqAmount;

		public PermissionsConfig Permissions;

		public object Clone()
		{
			var clone = (GuildConfig)this.MemberwiseClone();
			clone.StickyRoles = StickyRoles != null ? new Dictionary<ulong, IList<ulong>>(StickyRoles) : new Dictionary<ulong, IList<ulong>>();
			clone.StatusAddresses = StatusAddresses != null ? new Dictionary<string, string>(StatusAddresses) : new Dictionary<string, string>();
			clone.StatusAddressesCache = StatusAddressesCache != null ? new Dictionary<string, string>(StatusAddressesCache) : new Dictionary<string, string>();
			clone.VoteDeleteImmune = VoteDeleteImmune != null ? new HashSet<ulong>(VoteDeleteImmune) : new HashSet<ulong>();
			clone.Tags = Tags != null ? new Dictionary<ulong, IList<KeyValTag>>(Tags) : new Dictionary<ulong, IList<KeyValTag>>();
			clone.UserRateLimitCounts = UserRateLimitCounts != null ? new Dictionary<ulong, uint>(UserRateLimitCounts) : new Dictionary<ulong, uint>();
			clone.VoteDeleteReqIds = VoteDeleteReqIds != null ? new HashSet<ulong>(VoteDeleteReqIds) : new HashSet<ulong>();
			clone.VoteDeleteWeights = VoteDeleteWeights != null ? new Dictionary<ulong, uint>(VoteDeleteWeights) : new Dictionary<ulong, uint>();
			//clone.VoteDeleteReqAmount = VoteDeleteReqAmount;

			if (Permissions != null)
			{
				clone.Permissions = (PermissionsConfig)Permissions.Clone();
			}
			else
			{
				clone.Permissions = new PermissionsConfig();
				clone.Permissions.ValidataData();
			}
			return clone;
		}

		public bool ValidataData()
		{
			var clone = (GuildConfig)Clone();

			if (StickyRoles == null)
				StickyRoles = new Dictionary<ulong, IList<ulong>>();

			if (StatusAddresses == null)
				StatusAddresses = new Dictionary<string, string>();

			StatusAddresses = StatusAddresses.Where(x => !string.IsNullOrEmpty(x.Value)).ToDictionary(x => x.Key, x => x.Value);

			if (StatusAddressesCache == null)
				StatusAddressesCache = new Dictionary<string, string>();

			StatusAddressesCache = StatusAddressesCache.Where(x => !string.IsNullOrEmpty(x.Value)).ToDictionary(x => x.Key, x => x.Value);

			if (VoteDeleteImmune == null)
				VoteDeleteImmune = new HashSet<ulong>();

			if (Tags == null)
				Tags = new Dictionary<ulong, IList<KeyValTag>>();

			if (UserRateLimitCounts == null)
				UserRateLimitCounts = new Dictionary<ulong, uint>();

			if (VoteDeleteReqIds == null)
				VoteDeleteReqIds = new HashSet<ulong>();

			if (VoteDeleteWeights == null)
				VoteDeleteWeights = new Dictionary<ulong, uint>();

			if (VoteDeleteReqAmount == default(uint))
				VoteDeleteReqAmount = 5;

			if (Permissions == null)
				Permissions = new PermissionsConfig
				{
					Permissions = new Dictionary<string, ISet<ulong>>(),
					Admins = new HashSet<ulong>(),
					Blocked = new HashSet<ulong>()
				};
			else
				Permissions.ValidataData();

			return clone == this;
		}

		#region helpers
		public bool HasVoteDeleteIdWeight(ulong id)
			=> VoteDeleteWeights.ContainsKey(id);

		public bool HasVoteDeleteReqId(ulong id)
			=> VoteDeleteReqIds.Contains(id);

		public bool MatchesVoteDeleteRequirements(params IGuildUser[] users)
		{
			uint weight = 0;
			// loops the users and gets the highest possible weight and adds it
			foreach (var user in users)
			{
				var highestRole =
					user.RoleIds.Where(VoteDeleteWeights.ContainsKey).Select(x => VoteDeleteWeights[x])
						.OrderByDescending(x => x)
						.DefaultIfEmpty((uint)1)
						.First();

				uint highestId = 1;

				if (VoteDeleteWeights.ContainsKey(user.Id))
					highestId = VoteDeleteWeights[user.Id];

				weight += highestRole > highestId ? highestRole : highestId;
			}

			var ids = users.Select(x => x.Id);
			return weight >= VoteDeleteReqAmount && VoteDeleteReqIds.All(ids.Contains);
		}

		public bool AnyKeyName(string key)
			=> Tags.Any(x => x.Value.Any(y => y.Key.Contains(key)));

		public bool HasTagKey(ulong userId, string key)
			=> HasTags(userId) && Tags[userId].Any(x => x.Key.EqualsIgnoreCase(key));

		public bool HasTags(ulong userId)
			=> Tags.ContainsKey(userId);

		public bool IsVoteDeleteImmune(ulong id)
			=> Permissions.IsAdmin(id) || VoteDeleteImmune.Contains(id);

		public bool GiveVoteDeleteImmunity(ulong id)
			=> !IsVoteDeleteImmune(id) && VoteDeleteImmune.Add(id);

		public bool TakeVoteDeleteImmunity(ulong id)
			=> IsVoteDeleteImmune(id) && VoteDeleteImmune.Remove(id);

		public bool HasAddressName(string name)
			=> StatusAddresses.ContainsKey(name);

		public bool HasAdresses(params string[] addresses)
			=> addresses.All(StatusAddresses.Select(x => x.Value).Contains);

		public IEnumerable<ulong> GetStickyRoles(ulong userId)
			=> StickyRoles.Where(x => x.Value.Contains(userId)).Select(x => x.Key);

		public void CreateStickyRole(ulong roleId)
			=> StickyRoles.Add(roleId, new List<ulong>());

		public bool DeleteStickyRole(ulong roleId)
			=> StickyRoles.Remove(roleId);

		public bool IsStickyRole(ulong roleId)
			=> StickyRoles.ContainsKey(roleId);

		public bool HasStickyRole(ulong roleId, ulong userId)
			=> IsStickyRole(roleId) && StickyRoles[roleId].Contains(userId);

		public bool GiveStickyRole(ulong roleId, ulong userId)
		{
			if (!IsStickyRole(roleId))
				CreateStickyRole(roleId);
			StickyRoles[roleId].Add(userId);
			return true;
		}

		public bool TakeStickyRole(ulong roleId, ulong userId)
			=> IsStickyRole(roleId) && StickyRoles[roleId].Remove(userId);

		public bool IsStatusAdressCached(string key)
			=> StatusAddressesCache.ContainsKey(key);
		#endregion

		public async Task Update()
		{
			ValidataData();
			await ConfigManager.UpdateForGuild(this);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is GuildConfig && Equals((GuildConfig) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = GuildId.GetHashCode();
				hashCode = (hashCode * 397) ^ (StickyRoles != null ? StickyRoles.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (StatusAddresses != null ? StatusAddresses.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (StatusAddressesCache != null ? StatusAddressesCache.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (VoteDeleteImmune != null ? VoteDeleteImmune.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Tags != null ? Tags.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (UserRateLimitCounts != null ? UserRateLimitCounts.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (VoteDeleteReqIds != null ? VoteDeleteReqIds.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (VoteDeleteWeights != null ? VoteDeleteWeights.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (int) VoteDeleteReqAmount;
				hashCode = (hashCode * 397) ^ (Permissions != null ? Permissions.GetHashCode() : 0);
				return hashCode;
			}
		}

		public static bool operator ==(GuildConfig config1, GuildConfig config2)
		{
			return EqualityComparer<GuildConfig>.Default.Equals(config1, config2);
		}

		public static bool operator !=(GuildConfig config1, GuildConfig config2)
		{
			return !(config1 == config2);
		}

		public bool Equals(GuildConfig other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return GuildId == other.GuildId && Equals(StickyRoles, other.StickyRoles) && Equals(StatusAddresses, other.StatusAddresses) && Equals(VoteDeleteImmune, other.VoteDeleteImmune) && Equals(Tags, other.Tags) && Equals(UserRateLimitCounts, other.UserRateLimitCounts) && Equals(VoteDeleteReqIds, other.VoteDeleteReqIds) && Equals(VoteDeleteWeights, other.VoteDeleteWeights) && VoteDeleteReqAmount == other.VoteDeleteReqAmount && Equals(Permissions, other.Permissions);
		}
	}

}
