using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace tModloaderDiscordBot.Components
{
    public sealed class BotPermissions
    {
		[JsonProperty]
	    private readonly IDictionary<string, List<ulong>> _permissions = new Dictionary<string, List<ulong>>();
	    [JsonProperty]
		private readonly IList<ulong> _admins = new List<ulong>();
	    [JsonProperty]
		private readonly IList<ulong> _blocked = new List<ulong>();

	    public (int permissionsCount, int adminsCount, int blockedCount) ResetPermissions()
	    {
		    (int pC, int aC, int bC) counts = (_permissions.Count, _admins.Count, _blocked.Count); 
			_permissions.Clear();
		    _admins.Clear();
		    _blocked.Clear();
		    return counts;
	    }

	    public bool UpdatePermissionsForKey(string key, params ulong[] perms)
	    {
		    if (!HasPermissionsForKey(key)) return false;
		    _permissions[key.ToLowerInvariant()] = perms.ToList();
		    return true;
	    }

	    public IReadOnlyList<ulong> GetPermissionsForKey(string key)
	    {
			if (!HasPermissionsForKey(key)) return new List<ulong>().AsReadOnly();
		    return _permissions[key.ToLowerInvariant()].AsReadOnly();
	    }

	    public bool AddPermissionsForKey(string key, params ulong[] perms)
	    {
		    if (!HasPermissionsForKey(key)) return false;
		    _permissions[key.ToLowerInvariant()] = _permissions[key.ToLowerInvariant()].Union(perms).ToList();
		    return true;
		}

	    public bool RemovePermissionsForKey(string key, params ulong[] perms)
	    {
		    if (!HasPermissionsForKey(key)) return false;
		    _permissions[key.ToLowerInvariant()] = _permissions[key.ToLowerInvariant()].Except(perms).ToList();
		    return true;
		}

	    public bool AddNewPermission(string str)
	    {
		    str = str.ToLowerInvariant();
		    if (HasPermissionsForKey(str)) return false;

		    _permissions.Add(str, new List<ulong>());
		    return true;
	    }

	    public bool HasPermissionsForKey(string key)
		    => _permissions.ContainsKey(key.ToLowerInvariant());

		public void MakeAdmin(ulong userId)
		    => _admins.Add(userId);

	    public void RemoveAdmin(ulong userId)
		    => _admins.Remove(userId);

		public bool IsBlocked(ulong userId)
		    => _blocked.Contains(userId);

	    public bool IsAdmin(ulong userId)
		    => _admins.Contains(userId);

	    public bool HasPermission(string key, ulong id)
		    => IsAdmin(id) 
		       || !IsBlocked(id) 
		       && HasPermissionsForKey(key) 
		       && _permissions[key.ToLowerInvariant()].Contains(id);
    }
}
