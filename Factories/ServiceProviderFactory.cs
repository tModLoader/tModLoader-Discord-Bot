using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Resources;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Factories
{
	internal static class ServiceProviderFactory
	{
		private static Assembly EntryAssembly => Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

		private static readonly ResourceManager ResourceManager =
			new("tModloaderDiscordBot.Properties.Resources", EntryAssembly);

		public static IServiceCollection CreateServiceCollection()
		{
			return new ServiceCollection()
				.AddMediatR(typeof(Program))
				.AddSingleton<DiscordEventListener>()
				.AddSingleton<UserHandlerService>()
				.AddSingleton<CommandHandlerService>()
				.AddSingleton<HastebinService>()
				.AddSingleton<AutoPinService>()
				.AddSingleton<RecruitmentChannelService>()
				.AddSingleton<BanAppealChannelService>()
				.AddSingleton<SupportChannelAutoMessageService>()
				.AddSingleton<CrosspostService>()
				//.AddSingleton<ReactionRoleService>()
				// How to use resources:
				//_services.GetRequiredService<ResourceManager>().GetString("key")
				.AddSingleton(ResourceManager)
				.AddSingleton<LoggingService>()
				.AddSingleton<GuildConfigService>()
				.AddSingleton<SiteStatusService>()
				.AddSingleton<GuildTagService>()
				.AddSingleton<PermissionService>()
				.AddSingleton<LegacyModService>()
				.AddSingleton<ModService>()
				.AddSingleton<AuthorService>();
		}
	}
}