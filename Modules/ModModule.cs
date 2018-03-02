using System;
using System.Linq;
using System.Threading.Tasks;
using dtMLBot.Configs;
using dtMLBot.Preconditions;
using Discord;
using Discord.Commands;

namespace dtMLBot.Modules
{
	[Group("mod")]
	[Alias("modtools")]
	[HasPermission]
	public class ModModule : ConfigModuleBase<SocketCommandContext>
	{
		public ModModule(CommandService commandService) : base(commandService)
		{
		}

		[Command("unmute")]
		public async Task UnmuteAsync(IGuildUser user)
		{
			if (!Program.IsRateLimited(user.Id))
			{
				await ReplyAsync($"`{user.FullName()}` was not muted.");
				return;
			}

			Program.TakeRateLimit(user.Id);
			Program.UntrackRateLimit(user.Id);

			if (Config.UserRateLimitCounts.ContainsKey(user.Id))
			{
				Config.UserRateLimitCounts[user.Id] -= 1;
				if (Config.UserRateLimitCounts[user.Id] <= 0)
					Config.UserRateLimitCounts.Remove(user.Id);
				await Config.Update();
			}
			await ReplyAsync($"`{user.FullName()}` was unmuted.");
		}

		[Command("mute")]
		public async Task MuteAsync(IGuildUser user, uint minutes = 0)
		{
			var startTime = DateTimeOffset.UtcNow;
			var endTime = await Program.GiveRateLimit(user.Id, startTime, Config, minutes > 0 ? (uint?)minutes : null);

			await ReplyAsync($"{user.Mention}, you have been rate limited by {Context.User.FullName()} for {(endTime - startTime).TotalMinutes} minutes.");
		}

		[Command("softban")]
		[Alias("sban", "-sb")]
		public async Task SoftbanAsync(IGuildUser user, [Remainder]string reason = "")
		{
			// TODO hardcoded cuz out of time, make this configurable
			var role = Context.Guild.Roles.FirstOrDefault(x => x.Name.EqualsIgnoreCase("begone, evil!"));
			if (role == null)
			{
				await ReplyAsync($"Error getting softban role");
				return;
			}

			try
			{
				if (!user.RoleIds.Contains(role.Id))
				{
					await user.AddRoleAsync(role);
					var ch = Context.Guild.TextChannels.FirstOrDefault(x => x.Name.EqualsIgnoreCase("banappeal"));
					if (ch != null)
					{
						await ch.SendMessageAsync($"Moderator {Context.User.FullName()} softbanned {user.FullName()} with reason:" +
													$"\n```{(reason.Length > 0 ? reason : "No reason given")}```");
					}
				}
			}
			catch (Exception)
			{
				await ReplyAsync($"Role error on softban");
				return;
			}

		}

		[Group("immune")]
		[Alias("immunity")]
		public class ImmuneModule : ConfigModuleBase<SocketCommandContext>
		{
			public ImmuneModule(CommandService commandService) : base(commandService)
			{
			}

			[Command("add")]
			[Alias("-a")]
			public async Task AddAsync(IRole role)
				=> await AddAsync(role.Id);

			[Command("add")]
			[Alias("-a")]
			public async Task AddAsync(IGuildUser user)
				=> await AddAsync(user.Id);

			[Command("add")]
			[Alias("-a")]
			public async Task AddAsync(ulong id)
			{
				if (!Config.GiveVoteDeleteImmunity(id))
				{
					await ReplyAsync($"`{id}` was already marked immune");
					return;
				}

				await Config.Update();
				await ReplyAsync($"`{id}` is now marked immune");
			}

			[Command("delete")]
			[Alias("-d")]
			public async Task DeleteAsync(IRole role)
				=> await DeleteAsync(role.Id);

			[Command("delete")]
			[Alias("-d")]
			public async Task DeleteAsync(IGuildUser user)
				=> await DeleteAsync(user.Id);

			[Command("delete")]
			[Alias("-d")]
			public async Task DeleteAsync(ulong id)
			{
				if (!Config.TakeVoteDeleteImmunity(id))
				{
					await ReplyAsync($"`{id}` is not marked immune");
					return;
				}

				await Config.Update();
				await ReplyAsync($"`{id}` is no longer marked immune");
			}
		}

		[Group("stickyrole")]
		[Alias("sr")]
		public class StickyRoleModule : ConfigModuleBase<SocketCommandContext>
		{
			public StickyRoleModule(CommandService commandService) : base(commandService)
			{
			}

			[Command("create")]
			[Alias("add", "-a")]
			public async Task CreateAsync(IRole role)
			{
				if (Config.IsStickyRole(role.Id))
				{
					await ReplyAsync($"`{role.Name}` is already a sticky role.");
					return;
				}

				Config.CreateStickyRole(role.Id);
				await Config.Update();
				var txt = $"`{role.Name}` is now a sticky role.";
				var msg = await ReplyAsync(txt + "\nCounting users to be stickied...");

				try
				{
					var c = 0;
					await Context.Guild.DownloadUsersAsync();
					foreach (var user in Context.Guild.Users.Where(x => x.Roles.Contains(role)))
					{
						if (Config.GiveStickyRole(role.Id, user.Id))
							++c;
					}

					if (c > 0)
						await Config.Update();

					await msg.ModifyAsync(x =>
						x.Content = $"{txt}\n{c} users were stickied.");
				}
				catch (Exception)
				{
					await msg.ModifyAsync(x =>
						x.Content = $"{txt}\nFailed stickying users.");
				}
			}

			[Command("delete")]
			[Alias("-d")]
			public async Task DeleteAsync(IRole role, [Remainder]string args = "")
			{
				var clearArgs = args.Contains("-c") || args.Contains("-clear");

				if (!Config.IsStickyRole(role.Id))
				{
					if (clearArgs)
					{
						await ClearStickiedUsers(role);
						return;
					}
					await ReplyAsync($"`{role.Name}` is not a sticky role.");
					return;
				}

				var num = Config.IsStickyRole(role.Id) ? Config.StickyRoles[role.Id].Count : 0;

				Config.DeleteStickyRole(role.Id);
				await Config.Update();

				var txt = $"`{role.Name}` is no longer a sticky role.";
				if (num > 0)
					txt += $"\n{num} users had this role stickied to them.";

				await ReplyAsync(txt);

				if (clearArgs)
					await ClearStickiedUsers(role);
			}

			private async Task ClearStickiedUsers(IRole role)
			{
				var msg = await ReplyAsync($"Counting stickied users and clearing...");
				try
				{
					var c = 0;

					await Context.Guild.DownloadUsersAsync();
					foreach (var user in Context.Guild.Users.Where(x => x.Roles.Contains(role)))
					{
						await user.RemoveRoleAsync(role);
						++c;
					}

					await msg.ModifyAsync(x =>
						x.Content = $"Cleared `{c}` users of stickied role `{role.Name}`");
				}
				catch (Exception)
				{
					await msg.ModifyAsync(x =>
					x.Content = "Failed clearing stickied users. (Do I have permissions to change roles?)");
				}
			}
		}
	}
}
