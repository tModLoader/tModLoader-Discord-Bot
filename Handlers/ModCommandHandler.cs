using System;
using System.Linq;
using System.Threading.Tasks;
using tModloaderDiscordBot.Modules;
using tModloaderDiscordBot.Notifications;
using tModloaderDiscordBot.Services;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Handlers;

public class ModCommandHandler : BaseCommandHandler<ModCommandNotification>
{
	protected override async Task RunCommand(ModCommandNotification notification)
	{
		string? modName = notification.ModName.RemoveWhitespace();
		modName =
			ModService.Mods.FirstOrDefault(m =>
				string.Equals(m, modName, StringComparison.CurrentCultureIgnoreCase)) ?? null;

		if (modName == null)
		{
			await RespondAsync("Mod with that name doesn't exist", ephemeral: true);
			return;
		}

		var embed = await DefaultModule.GenerateModEmbed(modName, Context.Interaction.User);
		await RespondAsync(embed: embed);
	}
}