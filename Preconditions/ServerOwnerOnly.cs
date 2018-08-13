using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace tModloaderDiscordBot.Preconditions
{
	internal class ServerOwnerOnly : PreconditionAttribute
	{
		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			if (context.Guild == null || context.User == null)
				return Task.FromResult(PreconditionResult.FromError(""));

			if (context.Guild.OwnerId != context.User.Id)
				return Task.FromResult(PreconditionResult.FromError("User is not owner of guild"));

			return Task.FromResult(PreconditionResult.FromSuccess());
		}
	}
}
