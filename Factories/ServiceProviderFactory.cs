using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Resources;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Factories
{
	internal class ServiceProviderFactory
	{
		private ServiceProviderFactory() { }

		private static Assembly EntryAssembly => Assembly.GetEntryAssembly();
		private static readonly ResourceManager ResourceManager = new("tModloaderDiscordBot.Properties.Resources", EntryAssembly);

		public static IServiceCollection CreateServiceCollection()
		{
			return new ServiceCollection()
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
