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
		private string banAppealRoleName;
		private SocketRole banAppealRole;
		internal ITextChannel banAppealChannel;
		private bool _isSetup = false;

#if TESTBOT
		private const ulong banAppealChannelId = 1041803889105702923;
#else
		private const ulong banAppealChannelId = 331867286312845313;
#endif

		public BanAppealChannelService(IServiceProvider services) : base(services)
		{
			_client.GuildMemberUpdated += HandleGuildMemberUpdated;
		}

		internal async Task<bool> Setup()
		{
			if (!_isSetup)
				_isSetup = await Task.Run(() =>
				{
					banAppealChannel = (ITextChannel)_client.GetChannel(banAppealChannelId);
					// TODO Make this configurable
					banAppealRoleName = "BEGONE, EVIL!";
					banAppealRole = banAppealChannel.Guild.Roles.FirstOrDefault(x => x.Name == banAppealRoleName) as SocketRole;
					return true;
				});
			return _isSetup;
		}

		private async Task HandleGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
		{
			if (!await Setup())
				return;

			await before.GetOrDownloadAsync().ContinueWith(async task =>
			{
				if (after.Roles.Contains(banAppealRole) && !task.Result.Roles.Contains(banAppealRole))
				{
					var embed = new EmbedBuilder()
					.WithColor(Color.Blue)
					.WithDescription($"Welcome to {banAppealChannel.Mention} {after.Mention}. You have been placed here for violating a rule. Being placed here counts as a warning. If this is your first time here, if you promise to remember the rules and not do it again, we will let you out.")
					.Build();
					var botMessage = await banAppealChannel.SendMessageAsync("", embed: (Embed)embed);
				}
			});
		}
	}
}
