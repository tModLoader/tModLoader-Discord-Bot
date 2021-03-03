using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace tModloaderDiscordBot.Services
{
	class BanAppealChannelService : BaseService
	{
		private readonly DiscordSocketClient _client;
		private string banAppealRoleName;
		private SocketRole banAppealRole;
		internal ITextChannel banAppealChannel;

		public BanAppealChannelService(IServiceProvider services) : base(services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();
			_client.GuildMemberUpdated += HandleGuildMemberUpdated;
		}

		internal void Setup()
		{
#if TESTBOT
			banAppealChannel = (ITextChannel)_client.GetChannel(816493360722083851);
			banAppealRoleName = "banrole";
#else
			banAppealChannel = (ITextChannel)_client.GetChannel(331867286312845313);
			banAppealRoleName = "BEGONE, EVIL!";
#endif
			banAppealRole = banAppealChannel.Guild.Roles.FirstOrDefault(x => x.Name == banAppealRoleName) as SocketRole;
		}

		private async Task HandleGuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
		{
			if (banAppealChannel == null)
				return;

			if (after.Roles.Contains(banAppealRole))
				if (!before.Roles.Contains(banAppealRole)){
				{
					var embed = new EmbedBuilder()
					.WithColor(Color.Blue)
					.WithDescription($"Welcome to {banAppealChannel.Mention} {after.Mention}. You have been placed here for violating a rule. Being placed here counts as a warning. If this is your first time here, if you promise to remember the rules and not do it again, we will let you out.")
					.Build();
					var botMessage = await banAppealChannel.SendMessageAsync("", embed: (Embed)embed);
				}
			}
		}
	}
}
