using Discord;
using Discord.Interactions;
using System;
using System.Linq;
using System.Threading.Tasks;
using tModloaderDiscordBot.Services;
using System.Net;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Modules
{
	/// <summary>
	/// For commands using the interaction framework
	/// https://discordnet.dev/guides/int_framework/intro.html
	/// </summary>
	public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
	{
		[SlashCommand("mod", "Shows info about a mod")]
		public async Task Mod([Discord.Interactions.Summary("mod-name"), Autocomplete(typeof(ModNameAutocompleteHandler))] string modName)
		{
			modName = modName.RemoveWhitespace();
			modName = ModService.Mods.FirstOrDefault(m => string.Equals(m, modName, StringComparison.CurrentCultureIgnoreCase));
			if (modName == null)
			{
				await ReplyAsync($"Mod with that name doesn't exist");
				return;
			}

			Embed embed = await DefaultModule.GenerateModEmbed(modName, Context.Interaction.User);
			await RespondAsync("", embed: embed);
			//await RespondAsync($"Your choice: {parameterWithAutocompletion}", ephemeral: true);
		}

		[SlashCommand("ws", "Generates a search for a term in tModLoader wiki")]
		public async Task WikiSearch(string searchTerm)
		{
			searchTerm = searchTerm.Trim();
			string encoded = WebUtility.UrlEncode(searchTerm);
			await RespondAsync($"tModLoader Wiki results for {searchTerm}: <https://github.com/tModLoader/tModLoader/search?q={encoded}&type=Wikis>");
		}
	}

	public class ModNameAutocompleteHandler : AutocompleteHandler
	{
		public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
		{
			string userInput = autocompleteInteraction.Data.Current.Value.ToString();
			var mods = ModService.Mods.Where(m => m.Contains(userInput, StringComparison.CurrentCultureIgnoreCase)).Take(10).Select(x => new AutocompleteResult(x, x));
			return AutocompletionResult.FromSuccess(mods);
		}
	}
}
