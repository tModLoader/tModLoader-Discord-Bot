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
		public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
		{
            string? userInput = autocompleteInteraction.Data.Current.Value.ToString();
            if (userInput == null) {
	            return AutocompletionResult.FromError(new ArgumentNullException(nameof(userInput)));
            }
            
            var mods = ModService.Mods.Where(m => m.Contains(userInput, StringComparison.CurrentCultureIgnoreCase)).Take(25).Select(x => new AutocompleteResult(x, x));
            return AutocompletionResult.FromSuccess(mods);
		}
	}
}
