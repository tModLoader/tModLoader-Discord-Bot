using Discord;
using Discord.Commands;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Services;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Modules
{
	[Name("default")]
	public class DefaultModule : BotModuleBase
	{
		public LegacyModService LegacyModService { get; set; }
		public ModService ModService { get; set; }
		public AuthorService AuthorService { get; set; }
		
		//public BaseModule(CommandService commandService, GuildConfigService guildConfigService) : base(commandService, guildConfigService)
		//{
		//}

		[Command("help")]
		[Summary("Shows a list of commands")]
		[Remarks("Usage: `.help [command or alias]`\nExample: `.help em`")]
		public async Task Help([Remainder] string command = "")
		{
			EmbedBuilder e = new EmbedBuilder()
				.WithAuthor(new EmbedAuthorBuilder
				{
					IconUrl = Context.Message.Author.GetAvatarUrl(),
					Name = $"Requested by {Context.Message.Author.FullName()}"
				})
				.WithCurrentTimestamp();
			
			var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
			if (methods.Length == 0)
			{
				await ReplyAsync($"Error: No commands are defined");
				return;
			}
			
			if (!string.IsNullOrWhiteSpace(command))
			{
				var method = methods.FirstOrDefault(x => x.GetCustomAttribute<CommandAttribute>()?.Text == command ||
				                                         (x.GetCustomAttribute<AliasAttribute>()?.Aliases.Contains(command) ?? false));
				if (method is null)
				{
					await ReplyAsync($"Command `{command}` not found.");
					return;
				}

				e.Title = '.' + method.GetCustomAttribute<CommandAttribute>()!.Text;
				if (method.GetCustomAttribute<SummaryAttribute>() is { } descAttr)
				{
					e.AddField("Description", descAttr.Text);
				}

				if (method.GetCustomAttribute<AliasAttribute>() is { Aliases.Length: > 0 } aliasAttr)
				{
					e.AddField("Aliases", $".{string.Join(", .", aliasAttr.Aliases)}");
				}
				if (method.GetCustomAttribute<RemarksAttribute>() is { } remarksAttr)
				{
					e.AddField("Usage", remarksAttr.Text);
				}

				await ReplyAsync(embed: e.Build());
				return;
			}

			foreach (var method in methods)
			{
				var cmdAttr = method.GetCustomAttribute<CommandAttribute>();
				if (cmdAttr is null)
					continue;

				string title = '.' + cmdAttr.Text;
				if (method.GetCustomAttribute<AliasAttribute>() is { } aliasAttr)
				{
					title += $" (.{string.Join(", .", aliasAttr.Aliases)})";
				}

				string description = string.Empty;
				if (method.GetCustomAttribute<SummaryAttribute>() is { } descAttr)
				{
					description = descAttr.Text;
				}

				if (method.GetCustomAttribute<RemarksAttribute>() is { } remarksAttr)
				{
					description += $"\n{remarksAttr.Text}";
				}

				e.AddField(title, description);
			}

			await ReplyAsync(embed: e.Build());
		}

		[Command("ping")]
		[Summary("Returns the bot response time")]
		[Remarks("Usage: `.ping`")]
		public async Task Ping([Remainder] string _ = null)
		{
			string GetDeltaString(long elapsedTime, int latency) => $"\nMessage response time: `{elapsedTime} ms`" +
																	$"\nDelta: `{Math.Abs(elapsedTime - latency)} ms`";

			var clientLatency = Context.Client.Latency;
			string baseString = $"Latency: `{clientLatency} ms`";

			var msg = await ReplyAsync(baseString);

			var sw = Stopwatch.StartNew();

			await msg.ModifyAsync(p => p.Content =
				baseString +
				"\nMessage response time:" +
				"\nDelta:");

			sw.Stop();
			var elapsed = sw.ElapsedMilliseconds;
			await msg.ModifyAsync(x => x.Content =
				baseString +
				GetDeltaString(elapsed, clientLatency));
		}

		[Command("widget-legacy")]
		[Alias("widgetimg-legacy", "widgetimage-legacy", "widget13")]
		[Summary("Generates a widget image of specified mod")]
		[Remarks("Usage: `.widget13 <mod>`\nExample: `.widget13 examplemod`")]
		public async Task LegacyWidget([Remainder]string mod)
		{
			mod = mod.RemoveWhitespace();
			(bool result, string str) = await ShowSimilarMods(mod, true);
			
			string modFound = LegacyModService.Mods.FirstOrDefault(x => x.EqualsIgnoreCase(mod));
			if (modFound == null)
			{
				return;
			}

			var msg = await ReplyAsync($"Generating widget for {modFound}...");
			
			using var client = new System.Net.Http.HttpClient();
			byte[] response = await client.GetByteArrayAsync($"{LegacyModService.WidgetUrl}{modFound}");
			if (response == null)
			{
				await ReplyAsync($"Unable to create the widget");
				await msg.DeleteAsync();
				return;
			}

			using var stream = new MemoryStream(response);
			await Context.Channel.SendFileAsync(stream, $"widget-{modFound}.png");

			await msg.DeleteAsync();
		}
		
		[Command("widget")]
		[Alias("widgetimg", "widgetimage")]
		[Summary("Generates a widget image of specified mod")]
		[Remarks("Usage: `.widget <mod name or mod id>`\nExample: `.widget examplemod`")]
		public async Task Widget([Remainder]string mod)
		{
			mod = mod.RemoveWhitespace();

			using var client = new System.Net.Http.HttpClient();
			byte[] response;
			
			var msg = await ReplyAsync($"Generating widget for {mod}...");
			try
			{
				response = await client.GetByteArrayAsync($"{ModService.WidgetUrl}{mod}");
				if (response == null)
				{
					await ReplyAsync($"Unable to create the widget");
					return;
				}
			}
			catch
			{
				await ReplyAsync($"No mod with the name '{mod}' found!");
				await msg.DeleteAsync();
				return;
			}
			
			using var stream = new MemoryStream(response);
			await Context.Channel.SendFileAsync(stream, $"widget-{mod}.png");

			await msg.DeleteAsync();
		}

		[Command("author-widget")]
		[Alias("author-widgetimg", "author-widgetimage")]
		[Summary("Generates a widget image of the specified author")]
		[Remarks("Usage: `.author-widget <steamid64 or steam name (unreliable)>`\nExample: `.author-widget 76561198278789341`")]
		public async Task AuthorWidget([Remainder]string steamid)
		{
			steamid = steamid.RemoveWhitespace();
			
			using var client = new System.Net.Http.HttpClient();
			byte[] response;
			
			var msg = await ReplyAsync($"Generating widget for {steamid}...");
			try
			{
				response = await client.GetByteArrayAsync($"{AuthorService.WidgetUrl}{steamid}");
				if (response == null)
				{
					await ReplyAsync($"Unable to create the widget");
					await msg.DeleteAsync();

					return;
				}
			}
			catch
			{
				await ReplyAsync($"No author with the name or ID '{steamid}' found!");
				await msg.DeleteAsync();

				return;
			}

			using var stream = new MemoryStream(response);
			await Context.Channel.SendFileAsync(stream, $"widget-{steamid}.png");

			await msg.DeleteAsync();
		}
		
		[Command("author-widget-legacy")]
		[Alias("author-widgetimg-legacy", "author-widgetimage-legacy", "author-widget13")]
		[Summary("Generates a widget image of the specified author")]
		[Remarks("Usage: `.author-widget13 <steamid64 or steam name (unreliable)>`\nExample: `.author-widget13 76561198278789341`")]
		public async Task LegacyAuthorWidget([Remainder]string steamid)
		{
			steamid = steamid.RemoveWhitespace();
			var msg = await ReplyAsync($"Generating widget for {steamid}...");
			
			using var client = new System.Net.Http.HttpClient();
			byte[] response;
			try
			{
				response = await client.GetByteArrayAsync($"{AuthorService.LegacyWidgetUrl}{steamid}");
				if (response == null)
				{
					await ReplyAsync($"Unable to create the widget");
					await msg.DeleteAsync();

					return;
				}
			}
			catch
			{
				await ReplyAsync($"No author with the name or ID '{steamid}' found!");
				await msg.DeleteAsync();

				return;
			}

			using var stream = new MemoryStream(response);
			await Context.Channel.SendFileAsync(stream, $"widget-{steamid}.png");

			await msg.DeleteAsync();
		}

		[Command("wikis")]
		[Alias("ws")]
		[Summary("Generates a search for a term in tModLoader wiki")]
		[Remarks("Usage: `.wikis <search term>`\nExample: `.wikis TagCompound`")]
		public async Task WikiSearch([Remainder]string searchTerm)
		{
			searchTerm = searchTerm.Trim();
			string encoded = WebUtility.UrlEncode(searchTerm);
			await ReplyAsync($"tModLoader Wiki results for {searchTerm}: <https://github.com/tModLoader/tModLoader/search?q={encoded}&type=Wikis>");
		}

		[Command("examplemod")]
		[Alias("em", "example")]
		[Summary("Generates a search for a term in ExampleMod source code")]
		[Remarks("Usage: `.examplemod <search term>`\nExample: `.examplemod OnEnterWorld`")]
		public async Task ExampleModSearch([Remainder]string searchTerm)
		{
			searchTerm = searchTerm.Trim();
			string encoded = System.Net.WebUtility.UrlEncode(searchTerm);
			await ReplyAsync($"ExampleMod results for {searchTerm}: <https://github.com/tModLoader/tModLoader/search?utf8=✓&q={encoded}+path:ExampleMod&type=Code>");
		}

		[Command("microsoftdocs")]
		[Alias("msdn", "microsoft")]
		[Summary("Generates a search for a term in Microsoft documentation.")]
		[Remarks("Usage: `.msdn <search term>`\n`microsoft Int16`")]
		public async Task MsdnSearch([Remainder]string searchTerm)
		{
			searchTerm = searchTerm.Trim();
			var encoded = WebUtility.UrlEncode(searchTerm);
			await ReplyAsync($"Microsoft docs for {searchTerm}: <https://docs.microsoft.com/en-us/search/?scope=.NET&terms={encoded}>");
		}

		// Current classes documented on the Wiki
		static string[] vanillaClasses = new string[] { "item", "projectile", "tile", "npc" };
		static Dictionary<string, HashSet<string>> vanillaFields = new Dictionary<string, HashSet<string>>();

		[Command("documentation")]
		[Alias("doc", "docs")]
		[Summary("Generates a link to tModLoader or Terraria class documentation")]
		[Remarks("Usage: `.doc <classname>[.<field/method name>]`\nExample: `.doc Item.value`")]
		public async Task Documentation([Remainder]string searchTerm)
		{
			// TODO: use XML file to show inline documentation.
			var parts = searchTerm.Split(' ', '.');
			string className = parts[0].Trim();
			string classNameLower = className.ToLowerInvariant();
			string methodName = parts.Length >= 2 ? parts[1].Trim().ToLowerInvariant() : "";
			string methodNameLower = methodName.ToLowerInvariant();

			if (false && vanillaClasses.Contains(classNameLower))
			{
				if (methodName == "")
					await ReplyAsync($"Documentation for `{className}`: <https://github.com/tModLoader/tModLoader/wiki/{className}-Class-Documentation>");
				else
				{
					if (!vanillaFields.TryGetValue(classNameLower, out var fields))
					{
						fields = new HashSet<string>();
						//using (var client = new WebClient())
						//{
						//string response = await client.DownloadStringTaskAsync($"https://github.com/tModLoader/tModLoader/wiki/{className}-Class-Documentation");
						HtmlWeb hw = new HtmlWeb();
						HtmlDocument doc = await hw.LoadFromWebAsync($"https://github.com/tModLoader/tModLoader/wiki/{className}-Class-Documentation");
						foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
						{
							HtmlAttribute att = link.Attributes["href"];
							if (att.Value.StartsWith("#") && att.Value.Length > 1)
								fields.Add(att.Value.Substring(1));
						}
						//}

						vanillaFields[classNameLower] = fields;
					}
					if (fields.Contains(methodNameLower))
						await ReplyAsync($"Documentation for `{className}.{methodName}`: <https://github.com/tModLoader/tModLoader/wiki/{className}-Class-Documentation#{methodNameLower}>");
					else
						await ReplyAsync($"Documentation for `{className}.{methodName}` not found");
				}
			}
			else
			{
				// might be a modded class:
				//https://docs.tmodloader.net/docs/stable/annotated_dup.js

				using (var client = new WebClient())
				{
					string response = await client.DownloadStringTaskAsync("https://docs.tmodloader.net/docs/stable/annotated_dup.js");
					response = string.Join("\n", response.Split("\n").Skip(1)).TrimEnd(';');
					var resultObject = JsonConvert.DeserializeObject<List<List<object>>>(response);
					//var stringResultsOnly = resultObject.Where(x => x.All(y => y is string));
					var stringResultsOnly = resultObject.Where(x => x.Count == 3 && x[0] is string && x[1] is string);
					List<List<string>> result = new List<List<string>>();
					foreach (var item in stringResultsOnly)
					{
						result.Add(new List<string>() { item[0] as string, item[1] as string, item[2] as string });
					}
					var r = result.Find(x => x[0].EqualsIgnoreCase(classNameLower));
					if (r != null)
					{
						className = r[0];
						if (methodName == "")
						{
							await ReplyAsync($"Documentation for `{className}`: https://docs.tmodloader.net/docs/stable/{r[1]}");
						}
						else if (r[2] == null) // null means the class has no documentation, but class exists.
						{
							await ReplyAsync($"Documentation for `{className}.{methodName}` not found");
						}
						else
						{
							Console.WriteLine("https://docs.tmodloader.net/docs/stable/{r[2]}.js");
							// now to find method name
							response = await client.DownloadStringTaskAsync($"https://docs.tmodloader.net/docs/stable/{r[2]}.js");
							response = string.Join("\n", response.Split("\n").Skip(1)).TrimEnd(';');
							result = JsonConvert.DeserializeObject<List<List<string>>>(response);
							r = result.Find(x => x[0].EqualsIgnoreCase(methodNameLower));
							if (r != null)
							{
								methodName = r[0];
								await ReplyAsync($"Documentation for `{className}.{methodName}`: https://docs.tmodloader.net/docs/stable/{r[1]}");
							}
							else
							{
								await ReplyAsync($"Documentation for `{className}.{methodName}` not found");
							}
						}
					}
					else
					{
						if (methodName == "")
							await ReplyAsync($"Documentation for `{className}` not found");
						else
							await ReplyAsync($"Documentation for `{className}.{methodName}` not found");
					}
				}
			}
		}

		[Command("mod-legacy")]
		[Alias("modinfo-legacy", "mod13")]
		[Summary("Shows info about a mod")]
		[Remarks("Usage: `.mod13 <internal modname>` --OR-- `mod13 <part of name>`\nExample: `.mod13 examplemod`")]
		[Priority(-99)]
		public async Task LegacyMod([Remainder] string mod)
		{
			mod = mod.RemoveWhitespace();

			if (mod.EqualsIgnoreCase(">count"))
			{
				await ReplyAsync($"Found `{LegacyModService.Mods.Count()}` cached mods");
				return;
			}

			var (result, str) = await ShowSimilarMods(mod, true);

			if (result)
			{
				if (string.IsNullOrEmpty(str))
				{
					// Fixes not finding files
					mod = LegacyModService.Mods.FirstOrDefault(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));
					if (mod == null)
						return;
				}
				else mod = str;

				// Some mod is found continue.
				var modjson = JObject.Parse(await FileUtils.FileReadToEndAsync(new SemaphoreSlim(1, 1), LegacyModService.ModPath(mod)));
				var eb = new EmbedBuilder()
					.WithTitle("Mod: ")
					.WithCurrentTimestamp()
					.WithAuthor(new EmbedAuthorBuilder
					{
						IconUrl = Context.Message.Author.GetAvatarUrl(),
						Name = $"Requested by {Context.Message.Author.FullName()}"
					});

				foreach (var property in modjson.Properties().Where(x => !string.IsNullOrEmpty(x.Value.ToString())))
				{
					var name = property.Name;
					var value = property.Value;

					if (name.EqualsIgnoreCase("displayname"))
					{
						eb.Title += value.ToString();
					}
					else if (name.EqualsIgnoreCase("downloads"))
					{
						eb.AddField("# of Downloads", $"{property.Value:n0}", true);
					}
					else if (name.EqualsIgnoreCase("updatetimestamp"))
					{
						eb.AddField("Last updated", DateTime.Parse($"{property.Value}").ToString("dddd, MMMMM d, yyyy h:mm:ss tt", new CultureInfo("en-US")), true);
					}
					else if (name.EqualsIgnoreCase("iconurl"))
					{
						eb.ThumbnailUrl = value.ToString();
					}
					else if (name.EqualsIgnoreCase("modloaderversion"))
					{
						eb.AddField("tModLoader Version", value.ToString().Split(" ")[1], true);
					}
					else
					{
						eb.AddField(name.FirstCharToUpper(), value, true);
					}
				}

				eb.AddField("Widget", $"<{LegacyModService.WidgetUrl}{mod}>", true);
				using (var client = new System.Net.Http.HttpClient())
				{
					var response = await client.GetAsync(LegacyModService.QueryHomepageUrl + mod);
					var postResponse = await response.Content.ReadAsStringAsync();
					if (!string.IsNullOrEmpty(postResponse) && !postResponse.StartsWith("Failed:"))
					{
						eb.Url = postResponse;
						eb.AddField("Homepage", $"<{postResponse}>", true);
					}
				}

				await ReplyAsync("", embed: eb.Build());
			}
		}

		[Command("mod")]
		[Alias("modinfo")]
		[Summary("Shows info about a mod")]
		[Remarks("Usage: `.mod <internal modname or mod id>` \nExample: `.mod 2831018225` --OR-- `.mod examplemod`")]
		[Priority(-99)]
		public async Task Mod([Remainder] string modName)
		{
			try
			{
				modName = modName.RemoveWhitespace();

				if (modName.EqualsIgnoreCase(">count"))
				{
					await ReplyAsync($"Found `{ModService.Mods.Count()}` cached mods");
					return;
				}

				var (result, str) = await ShowSimilarMods(modName, false);

				if (result)
				{
					if (string.IsNullOrEmpty(str))
					{
						// Fixes not finding files
						modName = ModService.Mods.FirstOrDefault(m => string.Equals(m, modName, StringComparison.CurrentCultureIgnoreCase));
						if (modName == null)
							return;
					}
					else modName = str;

					Embed embed = await GenerateModEmbed(modName, Context.Message.Author);
					await ReplyAsync("", embed: embed);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"{nameof(DefaultModule)}.{nameof(Mod)}: An error occured generating the embed: {e.Message}\n{e.StackTrace}");
				await ReplyAsync("an error occured generating the embed");
			}
		}

		public static async Task<Embed> GenerateModEmbed(string modName, Discord.WebSocket.SocketUser requestor)
		{
			// Some mod is found continue.
			string modFileData = await FileUtils.FileReadToEndAsync(new SemaphoreSlim(1, 1), ModService.ModPath(modName));
			var modData = new ModInfo(JObject.Parse(modFileData), true);

			/*
			var modJData = JObject.Parse(await FileUtils.FileReadToEndAsync(new SemaphoreSlim(1, 1), ModService.ModPath(modName)));

			// parse json into object
			var modData = new
			{
				displayName = modJData["display_name"]?.Value<string>(),
				internalName = modJData["internal_name"]?.Value<string>(),
				modID = modJData["mod_id"]?.Value<string>(),
				version = modJData["version"]?.Value<string>(),
				workshopIconURL = modJData["workshop_icon_url"]?.Value<string>(),
				author = modJData["author"]?.Value<string>(),
				authorID = modJData["author_id"]?.Value<string>(),
				downloads = modJData["downloads_total"]?.Value<int>(),
				views = modJData["views"]?.Value<int>(),
				favorited = modJData["favorited"]?.Value<int>(),
				voteData = modJData["vote_data"]?.Value<JToken>(),
				modside = modJData["modside"]?.Value<string>(),
				tmodloaderVersion = modJData["tmodloader_version"]?.Value<string>(),
				timeCreated = modJData["time_created"]?.Value<int>(),
				timeUpdated = modJData["time_updated"]?.Value<int>(),
				homepage = modJData["homepage"]?.Value<string>()
			};
			*/

			// create embed
			var eb = new EmbedBuilder()
				.WithTitle($"Mod: {modData.DisplayName} ({modData.InternalName}) {modData.NewestVersion}")
				.WithCurrentTimestamp()
				.WithAuthor(new EmbedAuthorBuilder
				{
					IconUrl = requestor.GetAvatarUrl(),
					Name = $"Requested by {requestor.FullName()}"
				})
				.WithUrl($"https://steamcommunity.com/sharedfiles/filedetails/?id={modData.WorkshopId}")
				.WithThumbnailUrl(modData.WorkshopIconURL);

			// add fields
			eb.AddField("Author",
				$"[{modData.AuthorName} ({modData.CreatorId})]" +
				$"(https://steamcommunity.com/profiles/{modData.CreatorId}/)");

			eb.AddField("Downloads", $"{modData.SubscriberCount:n0}", true);
			eb.AddField("Views", $"{modData.Views:n0}", true);
			eb.AddField("Favorites", $"{modData.FavoritedCount:n0}", true);

			// if vote data exists
			if (modData.VoteData != null)
			{
				// calculate star amount
				double fullStarCount = 5 * modData.VoteData.score;
				double emptyStarCount = 5 - fullStarCount;

				// concatinate star characters to string
				string s = string.Concat(
					new string(Enumerable.Repeat('\u2605', (int)Math.Round(fullStarCount)).ToArray()),
					new string(Enumerable.Repeat('\u2606', (int)Math.Round(emptyStarCount)).ToArray()));

				eb.AddField("Votes", s, true);
				eb.AddField("Upvotes", modData.VoteData.votes_up, true);
				eb.AddField("Downvotes", modData.VoteData.votes_down, true);
			}

			eb.AddField("Mod Side", modData.ModSide);
			eb.AddField("tModLoader Version", modData.NewestTmlVersion);

			eb.AddField("Last updated", $"<t:{modData.TimeUpdated}:d>", true);
			eb.AddField("Time created", $"<t:{modData.TimeCreated}:d>", true);

			// if tags are present
			/*if (modJData["tags"].HasValues)
			{
				var tags = modJData["tags"];
				var tagNames = tags?.Select(x => x["display_name"].Value<string>());
				eb.AddField("Tags", string.Join(", ", tagNames));
			}*/

			if (modData.Tags != null)
			{
				eb.AddField("Tags", string.Join(", ", modData.Tags));
			}

			if (!string.IsNullOrEmpty(modData.Homepage))
			{
				eb.AddField("Homepage", modData.Homepage);
			}

			var embed = eb.Build();
			return embed;
		}

		[Command("author-legacy")]
		[Alias("authorinfo-legacy", "author13")]
		[Summary("Shows info about an author")]
		[Remarks("Usage: `.author13 <steamid64 or steam name (unreliable)>`\nExample: `.author13 76561198278789341` --OR-- `.author13 NotLe0n`")]
		[Priority(-99)]
		public async Task LegacyAuthor([Remainder] string steamID)
		{
			try
			{
				string authorJson = await AuthorService.DownloadSingleLegacyData(steamID);
				JObject authorJData;
				try
				{
					authorJData = JObject.Parse(authorJson);
				}
				catch
				{
					await ReplyAsync($"An author with the ID \"{steamID}\" was not found.");
					Console.WriteLine($"{nameof(DefaultModule)}.{nameof(LegacyAuthor)}: Error when parsing json. Server response was:\n{authorJson}");
					return;
				}

				var authorData = new
				{
					steamID = authorJData["steam_id"]?.Value<string>(),
					steamName = authorJData["steam_name"]?.Value<string>(),
					total = authorJData["total"]?.Value<int>(),
					downloadsTotal = authorJData["downloads_total"]?.Value<int>(),
					downloadsYesterday = authorJData["downloads_yesterday"]?.Value<int>(),
					mods = authorJData["mods"]?.Values<JObject>(),
					maintainedMods = authorJData["maintained_mods"]?.Values<JObject>()
				};

				// create embed
				var eb = new EmbedBuilder()
					.WithTitle($"Author: {authorData.steamName}")
					.WithCurrentTimestamp()
					.WithAuthor(new EmbedAuthorBuilder
					{
						IconUrl = Context.Message.Author.GetAvatarUrl(),
						Name = $"Requested by {Context.Message.Author.FullName()}"
					})
					.WithUrl($"https://steamcommunity.com/profiles/{authorData.steamID}/");

				eb.AddField("Total mod count", authorData.total ?? 0, true);
				eb.AddField("Total downloads count", authorData.downloadsTotal ?? 0, true);
				eb.AddField("Daily download count", authorData.downloadsYesterday ?? 0, true);

				string mods = string.Join(", ", authorData.mods
					.Select(mod => mod?["display_name"]?.Value<string>()));
				
				eb.AddField("Mods", authorData.mods.Any() ? mods : "No mods");


				if (authorData.maintainedMods.Any()) {
					string maintainedMods = string.Join(", ", authorData.maintainedMods
						.Select(mod => mod?["internal_name"]?.Value<string>()));

					eb.AddField("Maintained Mods", maintainedMods);
				}

				var embed = eb.Build();
				await ReplyAsync("", embed: embed);
			}
			catch (Exception e)
			{
				Console.WriteLine($"{nameof(DefaultModule)}.{nameof(LegacyAuthor)}: An error occured generating the embed: {e.Message}\n{e.StackTrace}");
				await ReplyAsync("an error occured generating the embed");
			}
		}
		
		[Command("author")]
		[Alias("authorinfo")]
		[Summary("Shows info about an author")]
		[Remarks("Usage: `.author <steamid64 or steam name (unreliable)>`\nExample: `.author 76561198278789341` --OR-- `.author NotLe0n`")]
		[Priority(-99)]
		public async Task Author([Remainder] string steamID)
		{
			try
			{
				string authorJson = await AuthorService.DownloadSingleData(steamID);
				if(authorJson.StartsWith("No steamid found"))
				{
					await ReplyAsync(authorJson);
					return;
				}
				JObject authorJData;
				try
				{
					authorJData = JObject.Parse(authorJson);
				}
				catch
				{
					await ReplyAsync($"An author with the ID \"{steamID}\" was not found.");
					Console.WriteLine($"{nameof(DefaultModule)}.{nameof(Author)}: Error when parsing json. Server response was:\n{authorJson}");
					return;
				}

				var authorData = new
				{
					steamID = authorJData["steam_id"]?.Value<string>(),
					steamName = authorJData["steam_name"]?.Value<string>(),
					total = authorJData["total"]?.Value<int>(),
					totalDownloads = authorJData["total_downloads"]?.Value<int>(),
					totalFavorites = authorJData["total_favorites"]?.Value<int>(),
					totalViews = authorJData["total_views"]?.Value<int>(),
					mods = authorJData["mods"]?.Values<JObject>()
				};

				// create embed
				var eb = new EmbedBuilder()
					.WithTitle($"Author: {authorData.steamName}")
					.WithCurrentTimestamp()
					.WithAuthor(new EmbedAuthorBuilder
					{
						IconUrl = Context.Message.Author.GetAvatarUrl(),
						Name = $"Requested by {Context.Message.Author.FullName()}"
					})
					.WithUrl($"https://steamcommunity.com/profiles/{authorData.steamID}/");

				eb.AddField("Total mod Count", authorData.total ?? 0);
				eb.AddField("Total downloads Count", authorData.totalDownloads ?? 0, true);
				eb.AddField("Total view Count", authorData.totalViews ?? 0, true);
				eb.AddField("Total favorites Count", authorData.totalFavorites, true);

				/* Field max is 1024
				string mods = string.Join(", ", authorData.mods
					.Select(mod =>
						$"[{mod?["display_name"]?.Value<string>()}]" +
						$"(https://steamcommunity.com/sharedfiles/filedetails/?id={mod?["mod_id"]?.Value<string>()})"));
				*/

				var modsSB = new StringBuilder();
				bool first = true;
				foreach (var mod in authorData.mods)
				{
					string modLink = $"[{mod?["display_name"]?.Value<string>()}]" + $"(https://steamcommunity.com/sharedfiles/filedetails/?id={mod?["mod_id"]?.Value<string>()})";
					if (modsSB.Length + modLink.Length > 1000)
					{
						modsSB.Append(" ...And more");
						break;
					}
					if (!first)
					{
						modsSB.Append(", ");
					}
					first = false;
					modsSB.Append(modLink);
				}

				eb.AddField("Mods", modsSB.ToString());

				var embed = eb.Build();
				await ReplyAsync("", embed: embed);
			}
			catch (Exception e)
			{
				Console.WriteLine($"{nameof(DefaultModule)}.{nameof(Author)}: An error occured generating the embed: {e.Message}\n{e.StackTrace}");
				await ReplyAsync("an error occured generating the embed");
			}
		}
		
		// Helper method
		private async Task<(bool, string)> ShowSimilarMods(string mod, bool legacy)
		{
			var ModsList = legacy ? LegacyModService.Mods : ModService.Mods;
			var mods = ModsList.Where(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));

			if (mods.Any()) return (true, string.Empty);

			/*
			// This should find mods published within last few hours.
			var cached = await (legacy ? LegacyModService.TryCacheMod(mod) : ModService.TryCacheMod(mod));
			if (cached) return (true, string.Empty);
			*/

			const string msg = "Mod with that name doesn\'t exist";
			var modMsg = "\nNo similar mods found..."; ;

			// Find similar mods

			var similarMods =
				ModsList
					.Where(m => m.Contains(mod, StringComparison.CurrentCultureIgnoreCase)
								&& m.LevenshteinDistance(mod) <= m.Length - 2) // prevents insane amount of mods found
					.ToArray();

			if (similarMods.Any())
			{
				if (similarMods.Length == 1) return (true, similarMods.First());

				modMsg = "\nDid you possibly mean any of these?\n" + similarMods.PrettyPrint();
				// Make sure message doesn't exceed discord's max msg length
				if (modMsg.Length > 2000)
				{
					modMsg = modMsg.Cap(2000 - msg.Length);
					// Make sure message doesn't end with a half cut modname
					var index = modMsg.LastIndexOf(',');
					var lastModClean = modMsg.Substring(index + 1).Replace("`", "").Trim();
					if (ModsList.All(m => m != lastModClean))
						modMsg = modMsg.Substring(0, index);
				}
			}

			await ReplyAsync($"{msg}{modMsg}");
			return (false, string.Empty);
		}
	}
}
