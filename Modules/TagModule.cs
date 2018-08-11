using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using tModloaderDiscordBot.Configs;
using tModloaderDiscordBot.Preconditions;

namespace tModloaderDiscordBot.Modules
{
	[Group("tag")]
	[Alias("tags")]
	public class TagModule : ConfigModuleBase<SocketCommandContext>
	{
		public TagModule(CommandService commandService) : base(commandService)
		{
		}

		private async Task<bool> CheckKeyValidity(string key)
		{
			if (Format.Sanitize(key).Equals(key) && !key.Contains(" "))
				return true;

			await ReplyAsync($"Invalid key. Key must not contain any markdown and whitespace.");
			return false;
		}

		private void AppendTags(StringBuilder sb, IEnumerable<KeyValTag> tags, int page, int numPages)
		{
			sb.AppendLine($"Tags found -- Page {page}/{numPages} -- (Showing up to 10 per page)");
			foreach (var t in tags)
				sb.AppendLine($"{Context.Guild.GetUser(t.OwnerId).FullName()}: {t.Key} `.tag -g {t.OwnerId} {t.Key}`{(t.IsGlobal ? " (g)" : "")}");
		}

		private void Paginate(int max, ref int page, ref IEnumerable<KeyValTag> tags, ref int totalPages)
		{
			totalPages = (int)Math.Ceiling((float)max / 10f);

			if (page < 1)
				page = 1;
			else if (page > totalPages)
				page = totalPages;

			tags = tags.Skip(10 * page - 10).Take(10);
		}

		private async Task<IEnumerable<KeyValTag>> TryFindOtherTags(string key, bool findValueMatches = false, int pageParameter = 1)
		{
			IEnumerable<KeyValTag> tags;
			if (key.EqualsIgnoreCase("tags::global"))
			{
				tags = Config.Tags.SelectMany(x => x.Value.Where(y => y.IsGlobal));
			}
			else
			{
				tags =
					findValueMatches
						? Config.Tags.SelectMany(x => x.Value.Where(y => y.Key.Contains(key) || y.Value.Contains(key)))
						: Config.Tags.SelectMany(x => x.Value.Where(y => y.Key.EqualsIgnoreCase(key)));
			}

			var page = pageParameter;
			int totalPages = 0;
			Paginate(tags.Count(), ref page, ref tags, ref totalPages);

			if (!tags.Any())
			{
				await ReplyAsync($"Tag `{key}` was not found");
				return tags;
			}

			var sb = new StringBuilder();
			AppendTags(sb, tags, page, totalPages);

			await ReplyAsync(sb.ToString());
			return tags;
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
			if (!await CheckKeyValidity(key))
				return;

			if (!Config.HasTagKey(id, key))
			{
				await ReplyAsync($"Tag `{key}` not found");
				return;
			}

			var tag = Config.Tags[id].FirstOrDefault(x => x.Key.EqualsIgnoreCase(key));

			if (tag != null)
			{
				if (!Config.Permissions.IsAdmin(Context.User.Id) && !tag.IsEditor(Context.User.Id))
				{
					await ReplyAsync($"You are not permitted to edit tag `{key}`");
					return;
				}

				tag.Value = value;
				tag.LastEditor = Context.User.Id;
				await Config.Update();
				await ReplyAsync($"Tag `{key}` was updated");
			}
		}

		[Command("list")]
		[Alias("-l")]
		public async Task ListAsync(int page = 1)
		{
			if (!Config.Tags.Any())
			{
				await ReplyAsync($"No tags found.");
				return;
			}

			var tags = Config.Tags.SelectMany(x => x.Value);
			int totalPages = 0;
			Paginate(tags.Count(), ref page, ref tags, ref totalPages);

			var sb = new StringBuilder();
			AppendTags(sb, tags, page, totalPages);

			await ReplyAsync(sb.ToString());
		}

		[Command("find")]
		[Alias("-f")]
		public async Task FindAsync(IGuildUser user, string key = "", int page = 1)
		{
			if (!Config.HasTags(user.Id))
			{
				await ReplyAsync($"No tags found for {user.FullName()}");
				return;
			}

			// check if given key was given page...
			bool hasKey = key.Length > 0;
			IEnumerable<KeyValTag> tags = Config.Tags[user.Id];
			if (hasKey)
			{
				if (int.TryParse(key, out int keyNum))
				{
					page = keyNum;
				}
				else
				{
					tags = Config.Tags[user.Id].Where(x => x.Key.Contains(key));
				}
			}
			int totalPages = 0;
			Paginate(tags.Count(), ref page, ref tags, ref totalPages);

			var sb = new StringBuilder();
			AppendTags(sb, tags, page, totalPages);

			await ReplyAsync(sb.ToString());
		}

		[Command("find")]
		[Alias("-f")]
		public async Task FindAsync(string key, int page = 1)
		{
			if (!await CheckKeyValidity(key))
				return;

			await TryFindOtherTags(key, true, page);

			//var sb = new StringBuilder();
			//var closeTagsKeys = Config.Tags.SelectMany(x => x.Value.Where(y => y.Key.StartsWith(key) || y.Key.EndsWith(key))).Except(tags).Take(10);
			//var keyValTags = closeTagsKeys as KeyValTag[] ?? closeTagsKeys.ToArray();
			//if (keyValTags.Any())
			//{
			//	sb.AppendLine($"Close matching tags found: (first 10)");
			//	foreach (var t in keyValTags)
			//		sb.AppendLine($"{Context.Guild.GetUser(t.OwnerId).FullName()}: {t.Key} (`.tag -i {t.OwnerId} {t.Key}`)");

			//	await ReplyAsync(sb.ToString());
			//}

			//var closeTagsValues = Config.Tags.SelectMany(x => x.Value.Where(y => y.Value.EqualsIgnoreCase(key) || y.Value.Contains(key))).Except(tags).Except(keyValTags).Take(10);
			//var tagsValues = closeTagsValues as KeyValTag[] ?? closeTagsValues.ToArray();
			//if (tagsValues.Any())
			//{
			//	sb.AppendLine($"Close matching tags values found: (first 10)");
			//	foreach (var t in tagsValues)
			//		sb.AppendLine($"{Context.Guild.GetUser(t.OwnerId).FullName()}: {t.Key} (`.tag -i {t.OwnerId} {t.Key}`)");

			//	await ReplyAsync(sb.ToString());
			//}
		}

		[Command("info")]
		[Alias("-i")]
		public async Task InfoAsync(string key)
			=> await InfoAsync(Context.User.Id, key);

		[Command("info")]
		[Alias("-i")]
		public async Task InfoAsync(IGuildUser user, string key)
			=> await InfoAsync(user.Id, key);

		private async Task InfoAsync(ulong id, string key)
		{
			if (!await CheckKeyValidity(key))
				return;

			if (!Config.HasTagKey(id, key))
			{
				await TryFindOtherTags(key);
				return;
			}

			var tag = Config.Tags[id].FirstOrDefault(x => x.Key.EqualsIgnoreCase(key));
			var owner = Context.Guild.GetUser(tag.OwnerId);
			var eb = new EmbedBuilder
			{
				Title = "Tag",
				Description = tag.Key,
				ThumbnailUrl = owner.GetAvatarUrl(),
				Author = new EmbedAuthorBuilder
				{
					Name = $"Requested by {Context.User.Username}#{Context.User.Discriminator}",
					IconUrl = Context.User.GetAvatarUrl()
				},
				Timestamp = DateTimeOffset.UtcNow,
				Fields = new List<EmbedFieldBuilder>
				{
					new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "Owner",
						Value = $"{owner.Username}#{owner.Discriminator} ({tag.OwnerId})"
					},
					new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "# Editors",
						Value = tag.Editors.Count
					},
					new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "Content length",
						Value = tag.Value.Length
					},
					new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "First 5 editors",
						Value = tag.Editors.Count <= 0 ? "No editors" : string.Join("\n", tag.Editors.Take(5).Select(x =>
						{
							var u = Context.Guild.GetUser(x);
							return $"{u.Username}#{u.Discriminator}";
						}))
					},
					new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "Last edited by",
						Value = Context.Guild.GetUser(tag.LastEditor)?.FullName() ?? "Unknown"
					},
					new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "Get code",
						Value = $".tag -g {tag.OwnerId} {tag.Key}"
					},
				}
			};

			await ReplyAsync("", embed: eb.Build());
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
			if (!await CheckKeyValidity(key))
				return;

			if (!Config.HasTagKey(id, key))
			{
				await TryFindOtherTags(key);
				return;
			}

			var tag = Config.Tags[id].FirstOrDefault(x => x.Key.EqualsIgnoreCase(key));

			if (tag != null)
				await ReplyAsync($"{Format.Bold($"Tag: {tag.Key} (Owner: {Context.Guild.GetUser(tag.OwnerId).FullName()}")})" +
								 $"\n{tag.Value}");
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
			if (!await CheckKeyValidity(key))
				return;

			if (!Config.HasTagKey(id, key))
			{
				await TryFindOtherTags(key);
				return;
			}

			var tag = Config.Tags[id].FirstOrDefault(x => x.Key.EqualsIgnoreCase(key));

			if (tag != null)
			{
				tag.IsGlobal = toggle;
				await Config.Update();
				await ReplyAsync($"Tag `{key}` owned by {id} is {(toggle ? "now global" : "no longer global")}.");
			}
		}

		[Command("add")]
		[Alias("-a")]
		public async Task AddAsync(string key, [Remainder]string value)
		{
			if (!await CheckKeyValidity(key))
				return;

			if (!Config.HasTags(Context.User.Id))
				Config.Tags.Add(Context.User.Id, new List<KeyValTag>());
			else if (Config.HasTagKey(Context.User.Id, key))
			{
				await ReplyAsync($"You already own a tag named `{key}`");
				return;
			}

			Config.Tags[Context.User.Id].Add(new KeyValTag
			{
				OwnerId = Context.User.Id,
				Key = key,
				Value = value,
				LastEditor = Context.User.Id
			});
			await Config.Update();
			await ReplyAsync($"Tag `{key}` was added");
		}

		[Command("delete")]
		[Alias("-d")]
		public async Task DeleteAsync(string key)
		{
			if (!await CheckKeyValidity(key))
				return;

			if (!Config.HasTagKey(Context.User.Id, key))
			{
				await ReplyAsync($"Tag `{key}` was not found");
				return;
			}

			var tag = Config.Tags[Context.User.Id].FirstOrDefault(x => x.Key.EqualsIgnoreCase(key));
			//await ReplyAsync(tag.GetHashCode() + "\n" + Config.Tags[Context.User.Id].First().GetHashCode() + "\n" + tag.Equals(Config.Tags[Context.User.Id].First()));

			bool deleted = Config.Tags[Context.User.Id].Remove(tag);

			if (deleted)
				await Config.Update();
			await ReplyAsync(
				deleted
					? $"Tag `{key}` was removed"
					: $"Unknown error trying to delete tag `{key}`");
		}
	}
}