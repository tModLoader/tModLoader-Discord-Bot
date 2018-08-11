using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace tModloaderDiscordBot.Preconditions
{
    internal class ServerOwnerOnly : PreconditionAttribute
    {
	    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
	    {
		    if (context.Guild == null || context.User == null)
			    return PreconditionResult.FromError("");

			return context.Guild.OwnerId == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User is not owner of guild");
	    }
    }
}
