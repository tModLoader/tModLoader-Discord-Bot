using Discord;
using Discord.Interactions;
using System;
using System.Linq;
using System.Threading.Tasks;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Interactions
{
    public class ModNameAutocompleteHandler : AutocompleteHandler
    {
		public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
		{
            string userInput = autocompleteInteraction.Data.Current.Value.ToString();
            var mods = ModService.Mods.Where(m => m.Contains(userInput, StringComparison.CurrentCultureIgnoreCase)).Take(10).Select(x => new AutocompleteResult(x, x));
			return Task.FromResult(AutocompletionResult.FromSuccess(mods));
		}
	}
}
