using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace tModloaderDiscordBot.Components
{
	public class ModInfo
	{
		internal static Regex versionRegex = new Regex(@"(?:tModLoader v)?([\d.]+)", RegexOptions.Compiled);

		public JObject Json;
		//public Version Version;
		public (Version ModVersion, Version TmlVersion)[] Versions;
		public Version OldestTmlVersion;
		public Version NewestTmlVersion;
		public Version NewestVersion;
		public string InternalName;
		public string DisplayName;
		public string AuthorName;
		public int SubscriberCount;
		public ulong WorkshopId;
		public long Playtime;
		public string WorkshopIconURL;
		public ulong CreatorId;
		public int FavoritedCount;
		public int TimeCreated;
		public int TimeUpdated;
		public string ModSide;
		public string[] Tags;
		public string Homepage;
		public int Views;
		public VoteData VoteData;

		public ModInfo(JObject json, bool fromSteam = false)
		{
			if (fromSteam)
			{
				this.Json = json;

				// NOTE: Should probably just use Paste as JSON Class command next time.
				var kvtags = (JArray)json["kvtags"];

				InternalName = GetFromKVTags("name");// (string)json["internal_name"];
				DisplayName = (string)json["title"];
				AuthorName = GetFromKVTags("Author"); // (string)json["author"];
				SubscriberCount = (int)json["subscriptions"];
				WorkshopId = (ulong)json["publishedfileid"];
				Playtime = (long)json["lifetime_playtime"];
				WorkshopIconURL = (string)json["preview_url"];
				CreatorId = (ulong)json["creator"]; // TODO: double check these casts with the API docs.
				FavoritedCount = (int)json["favorited"];
				TimeCreated = (int)json["time_created"];
				TimeUpdated = (int)json["time_updated"];
				ModSide = GetFromKVTags("modside");
				Tags = ((JArray)json["tags"])?.Select(x => x["display_name"].Value<string>()).ToArray();
				Homepage = GetFromKVTags("homepage");
				Views = (int)json["views"];
				VoteData = json["vote_data"].ToObject<VoteData>();

				NewestVersion = new Version(0, 0); // not sure where version is in older uploads

				string versionSummary = GetFromKVTags("versionsummary");
				if (versionSummary == "")
				{
					OldestTmlVersion = NewestTmlVersion = ParseVersion(GetFromKVTags("modloaderversion"));
				}
				else
				{
					Versions = versionSummary.Split(';')
						.Select(pair => (ParseVersion(pair.Split(':')[1]), ParseVersion(pair.Split(':')[0])))
						.ToArray();
					OldestTmlVersion = Versions.Select(v => v.TmlVersion).Min();
					NewestTmlVersion = Versions.Select(v => v.TmlVersion).Max(); // TmlVersion from versions entry will be only year.month: 2023.4.
					NewestVersion = Versions.Select(v => v.ModVersion).Max(); // Should this be mod version corresponding to latest tml version, or just latest version?
				}

				if (string.IsNullOrWhiteSpace(AuthorName))
				{
					AuthorName = "Unknown";
				}

				string GetFromKVTags(string tagName)
				{
					var tag = kvtags.FirstOrDefault(x => (string)x["key"] == tagName);
					if (tag == null)
						return "";
					return (string)tag["value"];
				}

				return;
			}

			//string versionString = (string)json["tmodloader_version"];
			//var match = versionRegex.Match(versionString);

			//if (!match.Success || !Version.TryParse(match.Groups[1].Value, out var version))
			//{
			//	throw new InvalidOperationException($"Invalid version: '{versionString}'.");
			//}

			Json = json;
			//Version = version;
			InternalName = (string)json["internal_name"];
			DisplayName = (string)json["display_name"];
			AuthorName = (string)json["author"];
			SubscriberCount = (int)json["downloads_total"];
			WorkshopId = (ulong)json["mod_id"];
			Playtime = (long)json["playtime"];

			if (json.ContainsKey("versions"))
			{
				Versions = json["versions"]
					.Select(pair => (ParseVersion((string)pair["mod_version"]), ParseVersion((string)pair["tmodloader_version"])))
					.ToArray();
				OldestTmlVersion = Versions.Select(v => v.TmlVersion).Min();
				NewestTmlVersion = Versions.Select(v => v.TmlVersion).Max(); // TmlVersion from versions entry will be only year.month: 2023.4.
			}
			else
			{
				// If loading old json, it might only have tmodloader_version. It isn't correct, but every mod will show up in the ported listing the 1st time this happens.
				OldestTmlVersion = NewestTmlVersion = ParseVersion((string)json["tmodloader_version"]);
			}

			if (string.IsNullOrWhiteSpace(AuthorName))
			{
				AuthorName = "Unknown";
			}
		}

		Version ParseVersion(string versionString)
		{
			var match = versionRegex.Match(versionString);

			if (!match.Success || !Version.TryParse(match.Groups[1].Value, out var version))
			{
				throw new InvalidOperationException($"Invalid version: '{versionString}'.");
			}

			return version;
		}

		//public string WorkshopLink => "[goo](https://google.com)"; // $@"[{InternalName}](https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopId})";
		public string WorkshopLink => $"[{InternalName}](https://s.team/sf/{WorkshopId})";
		// public string WorkshopLink => $"[{InternalName}](https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopId})";

		public override string ToString()
		{
			return $"{InternalName} {NewestTmlVersion} {SubscriberCount}";
		}
	}

	public class VoteData
	{
		public int score;
		public int votes_up;
		public int votes_down;
	}
}