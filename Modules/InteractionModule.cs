using Discord.Interactions;
using System.Threading.Tasks;
using System.Net;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Interactions;

namespace tModloaderDiscordBot.Modules
{
	/// <summary>
	/// For commands using the interaction framework
	/// https://discordnet.dev/guides/int_framework/intro.html
	/// </summary>
	public class InteractionModule(
		IServiceScopeFactory serviceScope
	) : InteractionModuleBase<SocketInteractionContext>
	{
		/// <summary>
		/// The MediatR instance returned by dependency injection
		/// </summary>
		private readonly IMediator _mediator = serviceScope.CreateScope().ServiceProvider.GetRequiredService<IMediator>();

		[SlashCommand("mod", "Shows info about a mod")]
		public async Task Mod([Summary("mod-name"), Autocomplete(typeof(ModNameAutocompleteHandler))] string modName)
		{
			await _mediator.Publish(Context.ToModCommand(this, modName));
		}

		[SlashCommand("ws", "Generates a search for a term in tModLoader wiki")]
		public async Task WikiSearch(string searchTerm)
		{
			searchTerm = searchTerm.Trim();
			string encoded = WebUtility.UrlEncode(searchTerm);
			await RespondAsync(
				$"tModLoader Wiki results for {searchTerm}: <https://github.com/tModLoader/tModLoader/search?q={encoded}&type=Wikis>");
		}
	}
}