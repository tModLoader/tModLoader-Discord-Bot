using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using tModloaderDiscordBot.Factories;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot
{
	public class Program
	{
#if TESTBOT
		private static readonly string _envTokenKey = "TmlTestToken";
#else
		private static readonly string  _envTokenKey = "TmlBotToken";
#endif

		public static bool Ready;

		public static void Main(string[] args)
			=> new Program().RunAsync().GetAwaiter().GetResult();


		internal static IUser BotOwner;


		/// <summary>
		/// Returns a service from the service provider
		/// </summary>
		public T GetService<T>() => ServiceProvider.GetRequiredService<T>();

		/// <summary>
		/// The interaction service
		/// </summary>
		public InteractionService InteractionService { get; private set; }

		/// <summary>
		/// The command service
		/// </summary>
		public CommandService CommandService => _commandService.Value;

		/// <summary>
		/// The service collection
		/// </summary>
		public ServiceCollection ServiceCollection => _serviceCollection.Value as ServiceCollection;

		/// <summary>
		/// The service provider
		/// </summary>
		public ServiceProvider ServiceProvider => _serviceCollection.Value.BuildServiceProvider();

		/// <summary>
		/// The Discord Client instance
		/// </summary>
		public DiscordSocketClient DiscordClient => _client.Value as DiscordSocketClient;

		/// <summary>
		/// The Discord client, provided by the factory
		/// </summary>
		private readonly Lazy<IDiscordClient> _client = new(DiscordClientFactory.CreateDiscordSocketClient);

		/// <summary>
		/// The service provider, provided by the factory
		/// </summary>
		private readonly Lazy<CommandService> _commandService = new(CommandServiceFactory.CreateCommandService);

		/// <summary>
		/// The service provider, provided by the factory
		/// </summary>
		private readonly Lazy<IServiceCollection> _serviceCollection = new(ServiceProviderFactory.CreateServiceCollection);

		private string _token => Environment.GetEnvironmentVariable(_envTokenKey);


		/// <summary>
		/// Initializes the program and starts the bot
		/// </summary>
		private async Task RunAsync()
		{
			if (_token == null)
			{
				await Console.Out.WriteLineAsync("No token environment variable was found. The bot requires one to run. Did you make sure to have set the variable?");
				await Console.Out.WriteLineAsync("If you wish to save a token, paste it and press enter:");
				var input = await Console.In.ReadLineAsync();
				Environment.SetEnvironmentVariable(_envTokenKey, input);
			}

			await InstallServices();
			await InitializeServices();
			await AddEventHandlers();

			await LoginAsync();
			await StartAsync();

			Console.Title = $@"tModLoader Bot - {DateTime.Now}";
			await Console.Out.WriteLineAsync($"https://discordapp.com/api/oauth2/authorize?client_id=&scope=bot");
			await Console.Out.WriteLineAsync($"Start date: {DateTime.Now}");
			await Task.Delay(-1);
		}

		private Task InstallServices()
		{
			ServiceCollection.AddSingleton(DiscordClient);
			ServiceCollection.AddSingleton(CommandService);
			return Task.CompletedTask;
		}

		private async Task InitializeServices()
		{
			await GetService<CommandHandlerService>().InitializeAsync();
			GetService<LoggingService>().Initialize();
			GetService<AutoPinService>();
		}

		private Task AddEventHandlers()
		{
			DiscordClient.Ready += ClientReady;
			DiscordClient.GuildAvailable += ClientGuildAvailable;
			DiscordClient.LatencyUpdated += ClientLatencyUpdated;
			return Task.CompletedTask;
		}

		private async Task StartAsync()
		{
			await DiscordClient.StartAsync();
		}

		private async Task LoginAsync()
		{
			await DiscordClient.LoginAsync(TokenType.Bot, _token, validateToken: true);
		}

		private async Task ClientLatencyUpdated(int i, int j)
		{
			UserStatus newUserStatus = UserStatus.Online;

			switch (DiscordClient.ConnectionState)
			{
				case ConnectionState.Disconnected:
					newUserStatus = UserStatus.DoNotDisturb;
					break;
				case ConnectionState.Connecting:
					newUserStatus = UserStatus.Idle;
					break;
			}

			await DiscordClient.SetStatusAsync(newUserStatus);
		}

		private async Task ClientReady()
		{
			Ready = false;
			await DiscordClient.SetGameAsync("Bot is starting...");
			await DiscordClient.SetStatusAsync(UserStatus.DoNotDisturb);

			BotOwner = (await DiscordClient.GetApplicationInfoAsync()).Owner;

			await GetService<GuildConfigService>().SetupAsync();
			await GetService<SiteStatusService>().UpdateAsync();
			await GetService<ModService>().Initialize().Maintain();
			await GetService<LegacyModService>().Initialize().Maintain();
			//await _reactionRoleService.Maintain(_client);

			await GetService<LoggingService>().Log(new LogMessage(LogSeverity.Info, "ClientReady", "Done."));
			// await _client.SetGameAsync("tModLoader " + LegacyModService.tMLVersion); TODO: Report the latest stable automatically? Would need to retrieve it each launch since it changes frequently.
			await DiscordClient.SetGameAsync("tModLoader");
			await ClientLatencyUpdated(DiscordClient.Latency, DiscordClient.Latency);
#if !TESTBOT
			var botChannel = (ISocketMessageChannel)await DiscordClient.GetChannelAsync(242228770855976960);
			await botChannel.SendMessageAsync("Bot has started successfully.");
#endif

			InteractionService = new InteractionService(DiscordClient);
			await InteractionService.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), ServiceProvider);
#if TESTBOT
			await InteractionService.RegisterCommandsToGuildAsync(1236004871543718040); // replace this is testing on your own server.
#else
			await InteractionService.RegisterCommandsToGuildAsync(103110554649894912);
			// await InteractionService.RegisterCommandsGloballyAsync();
#endif
			DiscordClient.InteractionCreated += async interaction =>
			{
				var ctx = new SocketInteractionContext(DiscordClient, interaction);
				await InteractionService.ExecuteCommandAsync(ctx, ServiceProvider);
			};

			Ready = true;
		}

		private async Task ClientGuildAvailable(SocketGuild arg)
		{
			await GetService<RecruitmentChannelService>().SetupAsync();
			//await ServiceProvider.GetRequiredService<BanAppealChannelService>().SetupAsync();
			await GetService<SupportChannelAutoMessageService>().SetupAsync();
			await GetService<CrosspostService>().SetupAsync();
			return;
		}
	}
}
