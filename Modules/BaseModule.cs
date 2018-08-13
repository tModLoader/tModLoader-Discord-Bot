using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.Commands;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Modules
{
	[Name("default")]
	public class BaseModule : BotModuleBase
	{
		//public BaseModule(CommandService commandService, GuildConfigService guildConfigService) : base(commandService, guildConfigService)
		//{
		//}

		[Command("ping")]
		[Summary("Returns the bot response time")]
		[Remarks("ping")]
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
	}
}
