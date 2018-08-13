using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace tModloaderDiscordBot.Services
{
	// todo scheduled backups
	// todo write to logs daily logs with backups
	// https://stackoverflow.com/questions/16138345/scheduled-task-assistance-please
	public class LoggingService
	{
		private readonly CommandService _commandService;
		private readonly DiscordSocketClient _client;
		private readonly IServiceProvider _services;

		public LoggingService(IServiceProvider services)
		{
			_commandService = services.GetRequiredService<CommandService>();
			_client = services.GetRequiredService<DiscordSocketClient>();
			_services = services;
		}

		public void InitializeAsync()
		{
			_client.Log += Log;
			_commandService.Log += Log;
		}

		public Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}
	}
}
