using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Resources;
using System.Threading.Tasks;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot
{
	public class Program
	{
		public static bool Ready;

		public static void Main(string[] args)
			=> new Program().StartAsync().GetAwaiter().GetResult();

		internal static IUser BotOwner;
		private CommandService _commandService;
		private DiscordSocketClient _client;
		private IServiceProvider _services;
		private LoggingService _loggingService;
		//private ReactionRoleService _reactionRoleService;

		private async Task StartAsync()
		{
			IServiceProvider BuildServiceProvider()
			{
				return new ServiceCollection()
						.AddSingleton(_client)
						.AddSingleton(_commandService)
						.AddSingleton<UserHandlerService>()
						.AddSingleton<CommandHandlerService>()
						.AddSingleton<HastebinService>()
						.AddSingleton<RecruitmentChannelService>()
						.AddSingleton<BanAppealChannelService>()
						.AddSingleton<SupportChannelAutoMessageService>()
						//.AddSingleton<ReactionRoleService>()
						// How to use resources:
						//_services.GetRequiredService<ResourceManager>().GetString("key")
						.AddSingleton(new ResourceManager("tModloaderDiscordBot.Properties.Resources", GetType().Assembly))
						.AddSingleton<LoggingService>()
						.AddSingleton<GuildConfigService>()
						.AddSingleton<SiteStatusService>()
						.AddSingleton<GuildTagService>()
						.AddSingleton<PermissionService>()
						.AddSingleton<LegacyModService>()
						.AddSingleton<ModService>()
						.BuildServiceProvider();
			}

			_client = new DiscordSocketClient(new DiscordSocketConfig
			{
				GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
				AlwaysDownloadUsers = true,
				LogLevel = LogSeverity.Verbose,
				MessageCacheSize = 100
			});
			_commandService = new CommandService(new CommandServiceConfig
			{
				DefaultRunMode = RunMode.Async,
				CaseSensitiveCommands = false,
#if TESTBOT
				LogLevel = LogSeverity.Critical,
				ThrowOnError = true,
#else
				LogLevel = LogSeverity.Debug,
				ThrowOnError = false
#endif
			});

			_services = BuildServiceProvider();
			await _services.GetRequiredService<CommandHandlerService>().InitializeAsync();
			_services.GetRequiredService<HastebinService>();
			_services.GetRequiredService<LoggingService>().Initialize();

			_client.Ready += ClientReady;
			_client.GuildAvailable += ClientGuildAvailable;
			_client.LatencyUpdated += ClientLatencyUpdated;

			// Begin our connection once everything is hooked up and ready to go
			// Because this is async, this returns immediately (connection is handled on a separate thread by the con manager)
			await _client.StartAsync().ContinueWith(async _ =>
			{
#if TESTBOT
				await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TestBotToken"), validateToken: true);
#else
				await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TmlBotToken"), validateToken: true);
#endif
			});

			Console.Title = $@"tModLoader Bot - {DateTime.Now}";
			await Console.Out.WriteLineAsync($"https://discordapp.com/api/oauth2/authorize?client_id=&scope=bot");
			await Console.Out.WriteLineAsync($"Start date: {DateTime.Now}");
			await Task.Delay(-1);
		}

		private async Task ClientLatencyUpdated(int i, int j)
		{
			UserStatus newUserStatus = UserStatus.Online;

			switch (_client.ConnectionState)
			{
				case ConnectionState.Disconnected:
					newUserStatus = UserStatus.DoNotDisturb;
					break;
				case ConnectionState.Connecting:
					newUserStatus = UserStatus.Idle;
					break;
			}

			await _client.SetStatusAsync(newUserStatus);
		}

		private async Task ClientReady()
		{
			Ready = false;
			await _client.SetGameAsync("Bot is starting...");
			await _client.SetStatusAsync(UserStatus.DoNotDisturb);

			BotOwner = (await _client.GetApplicationInfoAsync()).Owner;

			await _services.GetRequiredService<GuildConfigService>().SetupAsync();
			await _services.GetRequiredService<SiteStatusService>().UpdateAsync();
			await _services.GetRequiredService<LegacyModService>().Initialize().Maintain();
			//await _reactionRoleService.Maintain(_client);

			await _services.GetRequiredService<LoggingService>().Log(new LogMessage(LogSeverity.Info, "ClientReady", "Done."));
			await _client.SetGameAsync("tModLoader " + LegacyModService.tMLVersion);
			await ClientLatencyUpdated(_client.Latency, _client.Latency);
#if !TESTBOT
			var botChannel = (ISocketMessageChannel)await _client.GetChannelAsync(242228770855976960);
			await botChannel.SendMessageAsync("Bot has started successfully.");
#endif
			Ready = true;
		}

		private async Task ClientGuildAvailable(SocketGuild arg)
		{
			await _services.GetRequiredService<RecruitmentChannelService>().SetupAsync();
			await _services.GetRequiredService<BanAppealChannelService>().Setup();
			await _services.GetRequiredService<SupportChannelAutoMessageService>().Setup();
			return;
		}
	}
}
