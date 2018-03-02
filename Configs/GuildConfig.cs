using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace dtMLBot.Configs
{
	public sealed class GuildConfig : ICloneable, IEquatable<GuildConfig>
	{
		public ulong GuildId;
		public IDictionary<ulong, ISet<ulong>> StickyRoles; // roles that are stickied -> reapplies when leaving and rejoining
		public IDictionary<string, string> StatusAddresses; // addresses that can be checked with .status
		public ISet<ulong> VoteDeleteImmune; // IDs immune to vote deletion
		public IDictionary<ulong, ISet<KeyValTag>> Tags; // stored tags, uid -> set of tags
		public IDictionary<ulong, uint> UserRateLimitCounts; // uid -> number of rate limits
		public ISet<ulong> VoteDeleteReqIds; // req ids (role/usr) for vote deletion
		public IDictionary<ulong, uint> VoteDeleteWeights; // id -> vote weight (admin=3 etc.)
		public uint VoteDeleteReqAmount;

		public PermissionsConfig Permissions;

		public object Clone()
		{
			var clone = (GuildConfig)this.MemberwiseClone();
			clone.StickyRoles = StickyRoles != null ? new Dictionary<ulong, ISet<ulong>>(StickyRoles) : new Dictionary<ulong, ISet<ulong>>();
			clone.StatusAddresses = StatusAddresses != null ? new Dictionary<string, string>(StatusAddresses) : new Dictionary<string, string>();
			clone.VoteDeleteImmune = VoteDeleteImmune != null ? new HashSet<ulong>(VoteDeleteImmune) : new HashSet<ulong>();
			clone.Tags = Tags != null ? new Dictionary<ulong, ISet<KeyValTag>>(Tags) : new Dictionary<ulong, ISet<KeyValTag>>();
			clone.UserRateLimitCounts = UserRateLimitCounts != null ? new Dictionary<ulong, uint>(UserRateLimitCounts) : new Dictionary<ulong, uint>();
			clone.VoteDeleteReqIds = VoteDeleteReqIds != null ? new HashSet<ulong>(VoteDeleteReqIds) : new HashSet<ulong>();
			clone.VoteDeleteWeights = VoteDeleteWeights != null ? new Dictionary<ulong, uint>(VoteDeleteWeights) : new Dictionary<ulong, uint>();
			//clone.VoteDeleteReqAmount = VoteDeleteReqAmount;

			if (Permissions != null)
				clone.Permissions = (PermissionsConfig)Permissions.Clone();
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
				StickyRoles = new Dictionary<ulong, ISet<ulong>>();

			if (StatusAddresses == null)
				StatusAddresses = new Dictionary<string, string>();

			StatusAddresses = StatusAddresses.Where(x => !string.IsNullOrEmpty(x.Value)).ToDictionary(x => x.Key, x => x.Value);

			if (VoteDeleteImmune == null)
				VoteDeleteImmune = new HashSet<ulong>();

			if (Tags == null)
				Tags = new Dictionary<ulong, ISet<KeyValTag>>();

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
			=> StickyRoles.Add(roleId, new HashSet<ulong>());

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
			return StickyRoles[roleId].Add(userId);
		}

		public bool TakeStickyRole(ulong roleId, ulong userId)
			=> IsStickyRole(roleId) && StickyRoles[roleId].Remove(userId);
		#endregion

		public async Task Update()
		{
			ValidataData();
			await ConfigManager.UpdateForGuild(this);
		}

		public bool Equals(GuildConfig other)
		{
			return other != null &&
				   GuildId == other.GuildId &&
				   EqualityComparer<IDictionary<string, string>>.Default.Equals(StatusAddresses, other.StatusAddresses) &&
				   EqualityComparer<IDictionary<ulong, ISet<ulong>>>.Default.Equals(StickyRoles, other.StickyRoles) &&
				   EqualityComparer<ISet<ulong>>.Default.Equals(VoteDeleteImmune, other.VoteDeleteImmune) &&
				   EqualityComparer<IDictionary<ulong, ISet<KeyValTag>>>.Default.Equals(Tags, other.Tags) &&
				   EqualityComparer<IDictionary<ulong, uint>>.Default.Equals(UserRateLimitCounts, other.UserRateLimitCounts) &&
				   EqualityComparer<ISet<ulong>>.Default.Equals(VoteDeleteReqIds, other.VoteDeleteReqIds) &&
				   EqualityComparer<IDictionary<ulong, uint>>.Default.Equals(VoteDeleteWeights, other.VoteDeleteWeights) &&
				   VoteDeleteReqAmount == other.VoteDeleteReqAmount &&
				   EqualityComparer<PermissionsConfig>.Default.Equals(Permissions, other.Permissions);
		}

		public override int GetHashCode()
		{
			var hashCode = 2077117607;
			hashCode = hashCode * -1521134295 + GuildId.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<IDictionary<string, string>>.Default.GetHashCode(StatusAddresses);
			hashCode = hashCode * -1521134295 + EqualityComparer<IDictionary<ulong, ISet<ulong>>>.Default.GetHashCode(StickyRoles);
			hashCode = hashCode * -1521134295 + EqualityComparer<ISet<ulong>>.Default.GetHashCode(VoteDeleteImmune);
			hashCode = hashCode * -1521134295 + EqualityComparer<IDictionary<ulong, ISet<KeyValTag>>>.Default.GetHashCode(Tags);
			hashCode = hashCode * -1521134295 + EqualityComparer<IDictionary<ulong, uint>>.Default.GetHashCode(UserRateLimitCounts);
			hashCode = hashCode * -1521134295 + EqualityComparer<ISet<ulong>>.Default.GetHashCode(VoteDeleteReqIds);
			hashCode = hashCode * -1521134295 + VoteDeleteReqAmount.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<PermissionsConfig>.Default.GetHashCode(Permissions);
			return hashCode;
		}

		public override bool Equals(object obj)
		{
			return obj is GuildConfig config && Equals(config);
		}

		public static bool operator ==(GuildConfig config1, GuildConfig config2)
		{
			return EqualityComparer<GuildConfig>.Default.Equals(config1, config2);
		}

		public static bool operator !=(GuildConfig config1, GuildConfig config2)
		{
			return !(config1 == config2);
		}
	}

}
