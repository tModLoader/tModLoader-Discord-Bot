using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using tModloaderDiscordBot.Preconditions;

namespace tModloaderDiscordBot.Modules
{
	[Group("update")]
	[HasPermission]
	public class UpdateModule : ConfigModuleBase<SocketCommandContext>
	{
		public UpdateModule(CommandService commandService) : base(commandService)
		{
		}

		[Command]
		[Priority(-99)]
		public async Task Default(string url)
		{
			var msg = await Context.Channel.SendMessageAsync($@"Trying to update from source ""<{url}>""...");

			try
			{
				await Context.Client.SetGameAsync($"Updating...", type: ActivityType.Playing);
				const string projectPath = "/home/jofairden/tmlbot";
				Bash($@"sh {Path.Combine(projectPath,"update.sh")} {url} > {Path.Combine(projectPath,"allout.txt")}");
			}
			catch (Exception)
			{
				// Discard PingExceptions and return false;
				await msg.ModifyAsync(x => x.Content = "Something went wrong when trying to update from source.");
			}
		}

		internal static string Bash(string cmd)
		{
			var startInfo = new ProcessStartInfo {
				FileName = "sh",
				Arguments = $@"-c ""{Encoding.UTF8.GetString(Encoding.Default.GetBytes(cmd))}""",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			var process = new Process() {
				StartInfo = startInfo
			};
			process.Start();
			string result = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			return result;
		}
	}
}
