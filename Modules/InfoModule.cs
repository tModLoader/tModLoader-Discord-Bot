using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace tModloaderDiscordBot.Modules
{
	[Group("info")]
    public class InfoModule : BotModuleBase
    {
	    [Command("server")]
	    [Alias("guild", "-g")]
	    public async Task ServerAsync()
	    {
		    var guild = Context.Guild;

		    var eb = new EmbedBuilder
		    {
			    Title = "Guild",
			    Description = $"{guild.Name}",
			    ThumbnailUrl = guild.IconUrl,
			    Author = new EmbedAuthorBuilder
			    {
				    Name = $"Requested by {Context.User.FullName()}",
				    IconUrl = Context.User.GetAvatarUrl()
			    },
			    Timestamp = DateTimeOffset.UtcNow,
			    Fields = new List<EmbedFieldBuilder>
			    {
				    new EmbedFieldBuilder
				    {
					    IsInline = true,
					    Name = "ID",
					    Value = guild.Id
				    },
				    new EmbedFieldBuilder
				    {
					    IsInline = true,
					    Name = "Created at",
					    Value = guild.CreatedAt
				    },
				    new EmbedFieldBuilder
				    {
					    IsInline = true,
					    Name = "Age",
					    Value = BotUtils.PrettyPrintTimespan(DateTimeOffset.Now.Subtract(guild.CreatedAt))
				    },
				    new EmbedFieldBuilder
				    {
					    IsInline = true,
					    Name = "Region",
					    Value = guild.VoiceRegionId
				    },
				    new EmbedFieldBuilder
				    {
					    IsInline = true,
					    Name = "Owner",
					    Value = $"{guild.Owner.Username}#{guild.Owner.Discriminator}"
				    },
				    new EmbedFieldBuilder
				    {
					    IsInline = true,
					    Name = "# Text channels",
					    Value = guild.TextChannels.Count
				    },
				    new EmbedFieldBuilder
				    {
					    IsInline = true,
					    Name = "# Voice channels",
					    Value = guild.VoiceChannels.Count
				    },
			    }
		    };

		    await ReplyAsync(string.Empty, embed: eb.Build());
	    }

		[Command]
		public async Task RoleAsync(IRole role)
		{
			await role.Guild.DownloadUsersAsync();
			var users = await role.Guild.GetUsersAsync();

			var eb = new EmbedBuilder
			{
				Title = "Role",
				Description = role.Name,
				Author = new EmbedAuthorBuilder
				{
					Name = $"Requested by {Context.User.Username}#{Context.User.Discriminator}",
					IconUrl = Context.User.GetAvatarUrl()
				},
				Color = role.Color,
				Timestamp = DateTimeOffset.UtcNow,
				Fields = new List<EmbedFieldBuilder>
				{
					new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "ID",
						Value = role.Id
					},
					//new EmbedFieldBuilder
					//{
					//	IsInline = true,
					//	Name = "Stickied",
					//	Value = Config.IsStickyRole(role.Id)
					//},
					new EmbedFieldBuilder
					{
						IsInline = true,
						Name = "Users",
						Value = users.Count(x => x.RoleIds.Contains(role.Id))
					},
				}
			};

			await ReplyAsync(string.Empty, embed: eb.Build());
		}

		[Command]
		public async Task UserAsync(IGuildUser user)
		{
			var eb = new EmbedBuilder
			{
				Title = "User",
				Description = $"{user.Username}#{user.Discriminator}",
				ThumbnailUrl = user.GetAvatarUrl(),
				Author = new EmbedAuthorBuilder
				{
					Name = $"Requested by {Context.User.Username}#{Context.User.Discriminator}",
					IconUrl = Context.User.GetAvatarUrl()
				},
				Timestamp = DateTimeOffset.UtcNow,
				Color = (user as SocketGuildUser)?.Roles.Where(x => !x.Name.EqualsIgnoreCase("@everyone")).OrderByDescending(x => x.Position).FirstOrDefault()?.Color,
				Fields = new List<EmbedFieldBuilder>
					{
						new EmbedFieldBuilder
						{
							IsInline = true,
							Name = "Name",
							Value = $"{user.Username}{(user.Nickname?.Length > 0 ? $" : {user.Nickname}" : "")}"
						},
						new EmbedFieldBuilder
						{
							IsInline = true,
							Name = "ID",
							Value = user.Id
						},
						new EmbedFieldBuilder
						{
							IsInline = true,
							Name = "IsBot",
							Value = user.IsBot
						},
						new EmbedFieldBuilder
						{
							IsInline = true,
							Name = "IsWebhook",
							Value = user.IsWebhook
						},
						//new EmbedFieldBuilder
						//{
						//	IsInline = true,
						//	Name = "Roles",
						//	Value = $"{user.RoleIds.Count} (stickied: {user.RoleIds.Count(Config.IsStickyRole)})"
						//},
					}
			};

			await ReplyAsync(string.Empty, embed: eb.Build());
		}
	}
}
