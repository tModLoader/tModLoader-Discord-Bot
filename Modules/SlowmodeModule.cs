using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using tModloaderDiscordBot.Preconditions;

namespace tModloaderDiscordBot.Modules
{
	[Group("slowmode")]
	public class SlowmodeModule : ConfigModuleBase
	{
		[Command("reset")]
		[HasPermission]
		public Task ResetAsync(ITextChannel channel = null)
			=> SetAsync(channel, 0);

		[Command("set")]
		[HasPermission]
		public Task SetAsync(int intervalInSeconds)
			=> SetAsync(null, intervalInSeconds);

		[Command("set")]
		[HasPermission]
		public async Task SetAsync(ITextChannel channel, int intervalInSeconds)
		{
			channel ??= Context.Channel as ITextChannel;

			if (channel == null)
			{
				await ReplyAsync("Invalid channel.");
				return;
			}

			const int MinValue = 0;
			const int MaxValue = 21600;

			if (intervalInSeconds < MinValue || intervalInSeconds > MaxValue)
			{
				await ReplyAsync($"Interval is out of range, must be [{MinValue}..{MaxValue}].");
				return;
			}

			try
			{
				await channel.ModifyAsync(p => p.SlowModeInterval = intervalInSeconds);
				await ReplyAsync($"Slowmode for this channel has been set to {intervalInSeconds} seconds");
			}
			catch
			{
				await ReplyAsync("Failed to modify slowmode, does the bot have sufficient permissions?");
				return;
			}
		}

		[Command]
		[Priority(-99)]
		public async Task Default([Remainder] string toCheckParam = "")
		{
			// Not sure why, but giving permissions wouldn't work unless there was a command without [HasPermission]:
			// admin: .perm add slowmode userid -> "slowmode is not a known command or module"
			// user: .slowmode reset -> "slowmode is not a known command or module" and then "User not found."
			var msg = await Context.Channel.SendMessageAsync("You do not have permission to use this...");
		}
	}
}
