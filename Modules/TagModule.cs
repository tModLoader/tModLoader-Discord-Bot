using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Preconditions;
using tModloaderDiscordBot.Services;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Modules
{
	//@todo add global/info command
	[Group("tag")]
	public class TagModule : ConfigModuleBase
	{
		internal static readonly Dictionary<ulong, Tuple<ulong, ulong>> DeleteableTags = new Dictionary<ulong, Tuple<ulong, ulong>>(); // bot message id, <requester user id, original request message>

		public TagModule(IServiceProvider services)
		{
			var _client = services.GetRequiredService<DiscordSocketClient>();

			_client.ReactionAdded += HandleReactionAdded;
		}

		public GuildTagService TagService { get; set; }

		protected override void BeforeExecute(CommandInfo command)
		{
			base.BeforeExecute(command);
			TagService.Initialize(Context.Guild.Id);
		}

		internal static void AppendTags(StringBuilder sb, IEnumerable<GuildTag> tags, int page, int numPages, SocketGuild guild)
		{
			sb.AppendLine($"Tags found -- Page {page}/{numPages} -- (Showing up to 10 per page)");

			for (int i = 0; i < tags.Count(); i++)
			{
				var t = tags.ElementAt(i);
				sb.AppendLine($"{i}) {guild.GetUser(t.OwnerId).FullName()}: {t.Name} `.tag -g {t.OwnerId} {t.Name}`{(t.IsGlobal ? " (g)" : "")}");
			}
		}

		internal static void Paginate(int max, ref int page, ref IEnumerable<GuildTag> tags, ref int totalPages)
		{
			totalPages = (int)Math.Ceiling((float)max / 10f);

			if (page < 1) page = 1;
			else if (page > totalPages) page = totalPages;

			tags = tags.Skip(10 * page - 10).Take(10);
		}

		[Command("list")]
		[Alias("-l")]
		[Priority(1)]
		public async Task ListAsync(IUser user, int page = 1) => await ListAsync(TagService.GetTags(user.Id), page);

		[Command("list")]
		[Alias("-l")]
		[Priority(2)]
		public async Task ListAsync(int page = 1) => await ListAsync(TagService.GuildTags, page);

		[Command("find")]
		[Alias("-f")]
		public async Task FindAsync(string predicate, int page = 1)
		{
			await ListAsync(TagService.GuildTags.Where(x => x.Name.Contains(predicate) || x.Value.Contains(predicate)), page);
		}

		private async Task ListAsync(IEnumerable<GuildTag> tags, int page)
		{
			if (!tags.Any())
			{
				await ReplyAsync($"No tags found.");
				return;
			}
			var allTags = new List<GuildTag>(tags);
			int totalPages = 0;
			Paginate(tags.Count(), ref page, ref tags, ref totalPages);

			var sb = new StringBuilder();
			AppendTags(sb, tags, page, totalPages, Context.Guild);

			var msg = await ReplyAsync(sb.ToString());

			//@ todo emoji response
			//Config.TagListCache.Add(new CachedTagList
			//{
			//	originalMessage = Context.Message,
			//	containedTags = allTags,
			//	currentPage = page,
			//	maxPages = totalPages,
			//	message = msg,
			//	expiryTime = DateTimeOffset.Now.AddMinutes(2)
			//});
		}

		[Command("add")]
		[Alias("-a")]
		public async Task AddAsync(string name, [Remainder]string value)
		{
			if (!GuildTag.IsKeyValid(name))
			{
				await ReplyAsync($"Key for tag is not valid. Make sure there are no spaces.");
				return;
			}

			if (TagService.HasTag(Context.User.Id, name))
			{
				await ReplyAsync($"You already own a tag named `{name}`");
				return;
			}

			name = name.ToLowerInvariant();
			await TagService.AddNewTag(Context.User.Id, name, value);
			await ReplyAsync($"Tag `{name}` was added");
		}

		[Command("delete")]
		[Alias("-d")]
		public async Task DeleteAsync(string key)
		{
			if (!GuildTag.IsKeyValid(key))
			{
				await ReplyAsync($"Key for tag is not valid. Make sure there are no spaces.");
				return;
			}

			if (!TagService.HasTag(Context.User.Id, key))
			{
				await ReplyAsync($"Tag `{key}` was not found");
				return;
			}

			var tag = TagService.GetTag(Context.User.Id, key.ToLowerInvariant());
			await TagService.RemoveTag(tag);
			await ReplyAsync($"Tag `{key}` was removed");
		}


		[Command("edit")]
		[Alias("-e")]
		public async Task EditAsync(string key, [Remainder] string value)
			=> await EditAsync(Context.User.Id, key, value);

		[Command("edit")]
		[Alias("-e")]
		public async Task EditAsync(IGuildUser user, string key, [Remainder] string value)
			=> await EditAsync(user.Id, key, value);

		[Command("edit")]
		[Alias("-e")]
		public async Task EditAsync(ulong id, string key, [Remainder] string value)
		{
			if (!GuildTag.IsKeyValid(key))
			{
				await ReplyAsync($"Key for tag is not valid. Make sure there are no spaces.");
				return;
			}

			if (!TagService.HasTag(id, key))
			{
				await ReplyAsync($"Tag `{key}` not found");
				return;
			}

			var tag = TagService.GetTag(id, key);

			if (tag != null && tag.IsOwner(id))
			{
				// @todo permissions

				//if (!Config.Permissions.IsAdmin(Context.User.Id) && !tag.IsEditor(Context.User.Id))
				//{
				//	await ReplyAsync($"You are not permitted to edit tag `{key}`");
				//	return;
				//}

				tag.Value = value;
				await TagService.RequestConfigUpdate();
				await ReplyAsync($"Tag `{key}` was updated");
			}
		}

		[Command]
		public async Task Default(IGuildUser user, string key)
			=> await GetAsync(user.Id, key);

		[Command]
		public async Task Default(string key)
			=> await GetAsync(Context.User.Id, key);

		[Command("get")]
		[Alias("-g")]
		public async Task GetAsync(string key)
			=> await GetAsync(Context.User.Id, key);

		[Command("get")]
		[Alias("-g")]
		public async Task GetAsync(IGuildUser user, string key)
			=> await GetAsync(user.Id, key);

		private async Task GetAsync(ulong id, string key)
		{
			if (!GuildTag.IsKeyValid(key))
			{
				await ReplyAsync($"Key for tag is not valid. Make sure there are no spaces.");
				return;
			}

			if (!TagService.HasTag(id, key))
			{
				await TryFindOtherTags(key);
				return;
			}

			var tag = TagService.GetTag(id, key);
			var msg = await ReplyAsync(WriteTag(tag, Context.Guild.GetUser(tag.OwnerId).FullName()));

			await msg.AddReactionAsync(new Emoji("❌"));
			DeleteableTags.Add(msg.Id, new Tuple<ulong, ulong>(Context.Message.Author.Id, Context.Message.Id));
		}

		internal static string WriteTag(GuildTag tag, string ownerName)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(Format.Bold($"Tag: {tag.Name} (Owner: {ownerName})"));
			sb.Append(tag.Value);
			return sb.ToString();
		}

		private async Task<IEnumerable<GuildTag>> TryFindOtherTags(string key, int page = 1)
		{
			IEnumerable<GuildTag> tags;
			if (key.EqualsIgnoreCase("tags::global"))
			{
				tags = TagService.GuildTags.Where(x => x.IsGlobal);
			}
			else
			{
				tags = TagService.GetTags(key);
			}

			await ListAsync(tags, page);
			return tags;
		}

		[Command("global")]
		[HasPermission]
		public async Task GlobalAsync(string key, bool toggle)
			=> await GlobalAsync(Context.User.Id, key, toggle);

		[Command("global")]
		[HasPermission]
		public async Task GlobalAsync(IGuildUser user, string key, bool toggle)
			=> await GlobalAsync(user.Id, key, toggle);

		private async Task GlobalAsync(ulong id, string key, bool toggle)
		{
			bool CheckKeyValidity() => Format.Sanitize(key).Equals(key) && !key.Contains(" ");

			if (!CheckKeyValidity())
			{
				await ReplyAsync($"Invalid key. Key must not contain any markdown and whitespace.");
				return;
			}

			if (!TagService.HasTag(id, key))
			{
				await TryFindOtherTags(key);
				return;
			}

			var tag = TagService.GetTags(id).FirstOrDefault(x => x.MatchesName(key));

			if (tag != null)
			{
				tag.IsGlobal = toggle;
				await Config.Update();
				await ReplyAsync($"Tag `{key}` owned by {id} is {(toggle ? "now global" : "no longer global")}.");
			}
		}

		private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
		{
			bool delete = false;
			if (DeleteableTags.TryGetValue(message.Id, out Tuple<ulong, ulong> originalMessageAuthorAndMessage) && reaction.User.Value is SocketGuildUser reactionUser)
			{
				if(originalMessageAuthorAndMessage.Item1 == reactionUser.Id && reaction.Emote.Equals(new Emoji("❌")))
				{
					delete = true;
				}
			}

			if (delete)
			{
				DeleteableTags.Remove(message.Id);
				await (await channel.GetMessageAsync(originalMessageAuthorAndMessage.Item2)).DeleteAsync();
				await (await message.GetOrDownloadAsync()).DeleteAsync();
			}
		}
	}
}
