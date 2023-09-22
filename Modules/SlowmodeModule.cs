using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using tModloaderDiscordBot.Preconditions;

namespace tModloaderDiscordBot.Modules
{
	[Group("slowmode")]
	[HasPermission]
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
			}
			catch
			{
				await ReplyAsync("Failed to modify slowmode, does the bot have sufficient permissions?");
				return;
			}
		}
	}
}
