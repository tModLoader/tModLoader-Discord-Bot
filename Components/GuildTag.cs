using Discord;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Components
{
	/// <summary>
	/// Defines a tag in a guild
	/// </summary>
	public sealed class GuildTag
	{
		/// <summary>
		/// The owner's snowflake Id
		/// </summary>
		public ulong OwnerId;

		/// <summary>
		/// The tag's name
		/// </summary>
		public string Name;

		/// <summary>
		/// The tag's value
		/// </summary>
		public string Value;

		/// <summary>
		/// Whether the tag is global
		/// </summary>
		public bool IsGlobal;

		/// <summary>
		/// Returns whether the given snowflake id is the owner of this tag
		/// </summary>
		public bool IsOwner(ulong id) => OwnerId == id;

		/// <summary>
		/// Returns whether the tag's name matches the given name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool MatchesName(string name) => Name.EqualsIgnoreCase(name);

		/// <summary>
		/// Returns whether the given key is valid for use after being sanitized
		/// </summary>
		public static bool IsKeyValid(string key) => Format.Sanitize(key).Equals(key) && !key.Contains(" ");
	}
}
