using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Configs;

namespace tModloaderDiscordBot
{
	public class Program
	{
		public static void Main(string[] args)
			=> new Program().MainAsync().GetAwaiter().GetResult();

		internal static IUser BotOwner;
		internal static IDictionary<ulong, DateTimeOffset> Cooldowns;
		internal static IDictionary<ulong, Tuple<List<SocketUserMessage>, TimeSpan>> RateLimits; // counts, id -> list<messages>, total timespan
		internal static IDictionary<ulong, DateTimeOffset> RateLimitedUsers; // actual mutes, id -> end time
		internal static ISet<ulong> VotesForRemoval;
		private CommandService _commands;
		private DiscordSocketClient _client;
		private IServiceProvider _services;

		private async Task MainAsync()
			=> await StartAsync();

		private async Task StartAsync()
		{
			Cooldowns = new ConcurrentDictionary<ulong, DateTimeOffset>();
			RateLimits = new ConcurrentDictionary<ulong, Tuple<List<SocketUserMessage>, TimeSpan>>();
			RateLimitedUsers = new ConcurrentDictionary<ulong, DateTimeOffset>();
			VotesForRemoval = new HashSet<ulong>();

			_client = new DiscordSocketClient();
			_commands = new CommandService();

			_services = new ServiceCollection()
				.AddSingleton(_client)
				.AddSingleton(_commands)
				.BuildServiceProvider();

			_client.Log += Log;
			_client.Ready += ClientReady;
			_client.GuildMemberUpdated += GuildMemberUpdated;
			_client.UserLeft += UserLeft;
			_client.UserJoined += UserJoined;
			_client.ReactionAdded += ReactionAdded;
			_client.LatencyUpdated += ClientLatencyUpdated;
			//_client.Connected += ClientConnected;

			await InstallCommandsAsync();

			// TODO token.txt
			var tokenPath = Path.Combine(Environment.CurrentDirectory, "bot.token");
			string token = "";

			if (!File.Exists(tokenPath))
			{
				await Log(new LogMessage(LogSeverity.Critical, "Startup", "No bot.token file found, token not present"));
				await Task.Delay(-1);
			}

			Console.Title = $"tModLoader Bot - {AppContext.BaseDirectory} - {await ModsManager.GetTMLVersion()}";
			await Console.Out.WriteLineAsync($"https://discordapp.com/api/oauth2/authorize?client_id=&scope=bot");
			await Console.Out.WriteLineAsync($"Start date: {DateTime.Now}");

			token = File.ReadAllText(tokenPath);
			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();

			await Task.Delay(-1);
		}

		private async Task ClientLatencyUpdated(int i, int j)
		{
			await _client.SetStatusAsync(
				_client.ConnectionState == ConnectionState.Disconnected || j > 500 ? UserStatus.DoNotDisturb
				: _client.ConnectionState == ConnectionState.Connecting || j > 250 ? UserStatus.Idle
				: UserStatus.Online);
		}

		private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channelParam, SocketReaction reaction)
		{
			// remove user reaction if rate limited
			if (reaction.User.IsSpecified
				&& IsRateLimited(reaction.UserId))
			{
				var msg = await cacheable.DownloadAsync();
				await msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value, new RequestOptions { AuditLogReason = $"User is rate limited until {RateLimitedUsers[reaction.User.Value.Id].ToString()}" });
			}
			// vote deletion here
			else if (channelParam is SocketTextChannel channel
					 && reaction.Emote.Name == "⛔")
			{
				GuildConfig config;
				if ((config = ConfigManager.GetManagedConfig(channel.Guild.Id)) == null)
					return;

				var msg = await cacheable.DownloadAsync();
				if (config.IsVoteDeleteImmune(msg.Author.Id)
					|| msg.Author is SocketGuildUser gu && gu.Roles.Any(x => config.IsVoteDeleteImmune(x.Id)))
					return;

				// get the emote, count the total reactions, and get those users
				var emote = msg.Reactions.FirstOrDefault(x => x.Key.Name.Equals("⛔"));
				var count = emote.Value.ReactionCount;
				var users = (await msg.GetReactionUsersAsync(emote.Key, limit: count))
					.Select(x => channel.Guild.GetUser(x.Id))
					.Where(x => x != null)
					.Cast<IGuildUser>();

				// if we match the requirements for vote removal, proceed
				if (config.MatchesVoteDeleteRequirements(users.ToArray()))
					await msg.DeleteAsync(new RequestOptions { AuditLogReason = "Message was voted to be deleted." });
			}
		}

		/// <summary>
		/// If a user had left and had sticky roles, readd the sticky roles
		/// </summary>
		private async Task UserJoined(SocketGuildUser user)
		{
			var guild = user.Guild;
			if (ConfigManager.IsGuildManaged(guild.Id))
			{
				GuildConfig config;
				if ((config = ConfigManager.GetManagedConfig(guild.Id)) == null)
					return;
				await user.AddRolesAsync(config.GetStickyRoles(user.Id).Select(x => guild.GetRole(x)));
			}
		}

		/// <summary>
		/// If a user leaves and had sticky roles which werent tracked, track them
		/// </summary>
		private async Task UserLeft(SocketGuildUser user)
		{
			var guild = user.Guild;
			if (ConfigManager.IsGuildManaged(guild.Id))
			{
				await ConfigManager.UpdateCacheForGuild(guild.Id);
				var config = ConfigManager.Cache[guild.Id];

				var stickies = config.StickyRoles.Select(x => x.Key);
				var updateNeeded = false;

				foreach (var sticky in stickies)
				{
					var role = guild.GetRole(sticky);
					if (!user.Roles.Contains(role) || config.HasStickyRole(sticky, user.Id))
						continue;

					config.GiveStickyRole(sticky, user.Id);
					updateNeeded = true;
				}

				if (updateNeeded)
					await config.Update();
			}
		}

		/// <summary>
		/// When a guild member updates, compare the sticky roles.
		/// Untrack removed stickies, and track added stickies.
		/// </summary>
		private async Task GuildMemberUpdated(SocketGuildUser oldUser, SocketGuildUser newUser)
		{
			var guild = oldUser.Guild;
			if (ConfigManager.IsGuildManaged(guild.Id))
			{
				GuildConfig config;
				if ((config = ConfigManager.GetManagedConfig(guild.Id)) == null)
					return;

				var hadStickiedRoles = oldUser.Roles.Where(x => config.IsStickyRole(x.Id));
				var hasStickiedRoles = newUser.Roles.Where(x => config.IsStickyRole(x.Id));
				var hadStickiedDiff = hadStickiedRoles.Except(hasStickiedRoles); // old ones
				var hasStickiedDiff = hasStickiedRoles.Except(hadStickiedRoles); // new ones
				var needsUpdate = hadStickiedDiff.Any() || hasStickiedDiff.Any();

				foreach (var stickyRole in hadStickiedDiff)
					config.TakeStickyRole(stickyRole.Id, oldUser.Id);

				foreach (var stickyRole in hasStickiedDiff)
					config.GiveStickyRole(stickyRole.Id, newUser.Id);

				if (needsUpdate)
					await config.Update();
			}
		}

		/// <summary>
		/// Once the client is ready:
		/// * setup owner var
		/// * initialize the config manager
		/// * setup configs for every guild if needed
		/// * run timers to do stuff with data
		/// </summary>
		private async Task ClientReady()
		{
			BotOwner = (await _client.GetApplicationInfoAsync()).Owner;
			await ConfigManager.Initialize();
			await ModsManager.Initialize();
			await ModsManager.Maintain(_client);

			foreach (var guild in _client.Guilds)
				if (!ConfigManager.IsGuildManaged(guild.Id))
					await ConfigManager.SetupForGuild(guild.Id);

			var timer = new Timer(async o =>
				{
					await Task.Run(async () =>
					{
						var now = DateTimeOffset.UtcNow;

						Cooldowns = Cooldowns
							.Where(x => now < x.Value)
							.ToDictionary(x => x.Key, x => x.Value);

						RateLimitedUsers = RateLimitedUsers
							.Where(x => now < x.Value)
							.ToDictionary(x => x.Key, x => x.Value);

						try
						{
							await ModsManager.Maintain(_client);
						}
						catch (Exception e)
						{
							await Log(new LogMessage(LogSeverity.Critical, "SystemMain", "", e));
						}
					});
				}, null,
				TimeSpan.FromMinutes(5),
				TimeSpan.FromMinutes(5));


			await _client.SetGameAsync($"tModLoader {ModsManager.GetTMLVersion()}", type: ActivityType.Playing);
		}

		//private async Task ClientConnected()
		//{

		//}

		private async Task InstallCommandsAsync()
		{
			_client.MessageReceived += MessageReceivedAsync;

			// Adds all modules dynamically
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
		}

		// TODO make tracking + cooldowns cleaner ... ?

		#region helpers
		private static void TrackRateLimit(ulong userId, SocketUserMessage msg)
		{
			var tracked = IsTracked(userId);
			// TODO: tuple stuff is ugly as hell here
			if (tracked)
				RateLimits[userId] = new Tuple<List<SocketUserMessage>, TimeSpan>(new List<SocketUserMessage> { msg }.Union(RateLimits[userId].Item1).ToList(), msg.Timestamp.Subtract(RateLimits[userId].Item1.Last().Timestamp));
			else
				RateLimits.Add(userId, new Tuple<List<SocketUserMessage>, TimeSpan>(new List<SocketUserMessage> { msg }, TimeSpan.Zero));
		}

		internal static bool UntrackRateLimit(ulong userId)
			=> IsTracked(userId) && RateLimits.Remove(userId);

		private static bool NeedsRateLimit(ulong userId)
			=> IsTracked(userId) && RateLimits[userId].Item1.Count >= 5 && RateLimits[userId].Item2.TotalSeconds <= 10;

		private static bool NeedsTrackingClear(ulong userId)
			=> IsTracked(userId) && RateLimits[userId].Item2.TotalSeconds > 10;

		private static bool IsTracked(ulong userId)
			=> RateLimits.ContainsKey(userId);

		internal static bool IsRateLimited(ulong userId)
			=> RateLimitedUsers.ContainsKey(userId);

		internal static async Task<DateTimeOffset> GiveRateLimit(ulong userId, DateTimeOffset start, GuildConfig config, uint? overrideTime = null)
		{
			DateTimeOffset end;

			// update config, and set end time
			if (config != null)
			{
				if (config.UserRateLimitCounts.ContainsKey(userId))
					config.UserRateLimitCounts[userId] += 1;
				else
					config.UserRateLimitCounts.Add(userId, 1);

				await config.Update();
				end = start.AddMinutes(config.UserRateLimitCounts[userId] * 20);
			}
			else
				end = start.AddMinutes(20);

			// mute command
			if (overrideTime.HasValue)
				end = start.AddMinutes(overrideTime.Value);

			// add mute to memory
			if (!IsRateLimited(userId))
				RateLimitedUsers.Add(userId, end);
			else
				RateLimitedUsers[userId] = end;

			return end;
		}

		internal static bool TakeRateLimit(ulong userId)
			=> RateLimitedUsers.ContainsKey(userId) && RateLimitedUsers.Remove(userId);

		private static void GiveCooldown(ulong userId, DateTimeOffset end)
		{
			if (!HasCooldown(userId))
				Cooldowns.Add(userId, end);
			else
				Cooldowns[userId] = end;
		}

		private static void TakeCooldown(ulong userId)
		{
			if (HasCooldown(userId))
				Cooldowns.Remove(userId);
		}

		private static bool HasCooldown(ulong userId)
			=> Cooldowns.ContainsKey(userId);

		private static bool CooldownReady(ulong userId, DateTimeOffset end)
			=> !HasCooldown(userId) || end >= Cooldowns[userId];
		#endregion

		/// <summary>
		/// Handles a user message
		/// * will keep track of user messages, and rate limit them when appropiate
		/// * will keep track of user cooldowns, and only let through commands if not on cooldown
		/// </summary>
		private async Task MessageReceivedAsync(SocketMessage messageParam)
		{
			try
			{
				if (!(messageParam is SocketUserMessage message)
					|| message.Author.IsBot
					|| message.Author.IsWebhook
					|| message.Channel is SocketDMChannel) return;

				var author = message.Author;
				TrackRateLimit(author.Id, message);

				if (IsRateLimited(author.Id))
				{
					await message.DeleteAsync(new RequestOptions { AuditLogReason = $"User is rate limited until {RateLimitedUsers[author.Id].ToString()}" });
					if (NeedsRateLimit(author.Id) && author is SocketGuildUser sgu)
						await sgu.KickAsync($"User repeatedly spammed while rate limited");
					return;
				}

				bool isAdmin = author.Id == BotOwner.Id;
				SocketTextChannel channel = message.Channel as SocketTextChannel;
				SocketGuild guild = channel?.Guild;
				GuildConfig config = null;

				if (channel != null)
				{
					if ((config = ConfigManager.GetManagedConfig(guild.Id)) == null)
						return;

					if (!isAdmin)
						isAdmin = config.Permissions.IsAdmin(message.Author.Id);
				}

				var context = new SocketCommandContext(_client, message);

				if (!isAdmin)
				{
					// we're not admin, and we've spammed. we get muted.
					if (NeedsRateLimit(author.Id))
					{
						var startTime = DateTimeOffset.UtcNow;
						var endTime = await GiveRateLimit(author.Id, startTime, config);
						bool notify = true;
						if (config != null && author is SocketGuildUser gu && config.UserRateLimitCounts[author.Id] > 3)
						{
							try
							{
								notify = false;
								var reason = $"User was automatically kicked after being rate limited {config.UserRateLimitCounts[author.Id]} times.";
								await gu.KickAsync(reason, new RequestOptions { AuditLogReason = reason });
							}
							catch (Exception)
							{
								// TODO notify no ability to kick. --> mute persists, no worries.
								// could not kick
								notify = true;
							}

						}

						if (notify)
							await context.Channel.SendMessageAsync($"{author.Mention}, you have been rate limited for {(endTime - startTime).TotalMinutes} minutes.");

						// go over messages of spam, delete them
						foreach (var msg in RateLimits.Select(x => x.Value).SelectMany(x => x.Item1))
							await msg.DeleteAsync(new RequestOptions { AuditLogReason = "User was automatically rate limited." });

						// clear
						UntrackRateLimit(author.Id);
						return;
					}

					if (!CooldownReady(message.Author.Id, DateTimeOffset.UtcNow))
						return;
				}

				if (NeedsTrackingClear(author.Id))
					UntrackRateLimit(author.Id);

				var argPos = 0;
				if (!(message.HasCharPrefix('.', ref argPos)
					/*|| message.HasMentionPrefix(_client.CurrentUser, ref argPos))*/))
					return;

				// give command cooldown
				GiveCooldown(message.Author.Id, DateTimeOffset.UtcNow.AddSeconds(3));

				// attempt execute
				var result = await _commands.ExecuteAsync(context, argPos, _services);
				// no success
				if (!result.IsSuccess)
				{
					// try looking for tags with this key
					if (channel != null)
					{
						var key = message.Content.Substring(1);
						var check = Format.Sanitize(key);
						if (check.Equals(key))
						{
							// we dont have this key, try looking for other people's keys
							if (!config.HasTagKey(author.Id, key))
							{
								if (config.AnyKeyName(key))
									result = await _commands.ExecuteAsync(context, $"tag -f {message.Content.Substring(1)}", multiMatchHandling: MultiMatchHandling.Exception);

								if (result.IsSuccess)
									return;
							}
							else
							{
								// we have this key, get ours
								//TakeCooldown(message.Author.Id);
								var tag = config.Tags[author.Id].First(x => x.Key.EqualsIgnoreCase(key));
								await channel.SendMessageAsync($"{Format.Bold($"Tag: {tag.Key}")}" +
															   $"\n{tag.Value}");
								return;
							}
						}
					}

					TakeCooldown(author.Id);

					// send the problem
					if (!result.ErrorReason.EqualsIgnoreCase("Unknown command."))
						await context.Channel.SendMessageAsync(result.ErrorReason);
				}
			}
			catch (Exception e)
			{
				await Log(new LogMessage(LogSeverity.Info, "self", e.ToString(), e));
			}
		}

		// TODO make proper logging service
		private static Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}
	}
}
