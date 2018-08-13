using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace tModloaderDiscordBot.Preconditions
{
	internal class HasPermissionAttribute : PreconditionAttribute
	{
		public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			if (context.Guild == null)
				return PreconditionResult.FromError("No guild provided");

			if (context.User.Id == context.Guild.OwnerId)
				return PreconditionResult.FromSuccess();

			var commandService = (CommandService)services.GetService(typeof(CommandService));
			if (commandService == null)
				return PreconditionResult.FromError($"Could not find command service from service provider");

			var cmd = await BotUtils.SearchCommand(commandService, context, command.Module.Name);
			bool moduleFound = cmd != null;
			if (!moduleFound)
				return PreconditionResult.FromError("Module not found");

			cmd = await BotUtils.SearchCommand(commandService, context, command.Name);
			bool commandFound = cmd != null;
			if (!commandFound)
				return PreconditionResult.FromError("Command not found");

			// todo future code goes here...
			bool hasPerm = false;
			if (!hasPerm)
				return PreconditionResult.FromError($"No permissions setup for the command `{cmd}` but required to use it.");

			return PreconditionResult.FromSuccess();
		}
	}
}
