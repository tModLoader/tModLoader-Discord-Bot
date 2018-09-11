using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Services;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Preconditions
{
	internal class HasPermissionAttribute : PreconditionAttribute
	{
		public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			bool CheckIfBlocked(IGuildUser user, BotPermissions gPerms)
			{
				return gPerms.IsBlocked(context.User.Id) 
				       || user.RoleIds.Any(gPerms.IsBlocked);
			}

			bool HasPermissions(string key, IGuildUser user, BotPermissions gPerms)
			{
				return gPerms.HasPermission(key, user.Id)
					   || user.RoleIds.Any(x => gPerms.HasPermission(key, x));
			}

			if (context.Guild == null)
				return PreconditionResult.FromError("No guild provided");

			if (context.User.Id == context.Guild.OwnerId)
				return PreconditionResult.FromSuccess();

			var commandService = (CommandService)services.GetService(typeof(CommandService));
			if (commandService == null)
				return PreconditionResult.FromError($"Could not find command service from service provider");

			// Get the permissions
			var permissionService = (PermissionService)services.GetService(typeof(PermissionService));
			if (permissionService == null)
				return PreconditionResult.FromError("PermissionsSerivce not found");
			permissionService.Initialize(context.Guild.Id);
			var permissions = permissionService.GetGuildPermissions();

			if (!(context.User is IGuildUser gUser))
				return PreconditionResult.FromError("User is not a IGuildUser");

			// If we are blocked, we cannot proceed.
			if (CheckIfBlocked(gUser, permissions))
				return PreconditionResult.FromError("User is blocked");

			// First, check if the module exists
			// If we have global (module) permission, proceed
			var cmd = await BotUtils.SearchCommand(commandService, context, command.Module.Name);
			bool moduleFound = cmd != null;
			if (!moduleFound)
				return PreconditionResult.FromError("Module not found");

			if (HasPermissions(cmd, gUser, permissions))
				return PreconditionResult.FromSuccess();

			// No global permission, but maybe permissions by command
			cmd = await BotUtils.SearchCommand(commandService, context, command.Name);
			if (cmd == null)
				return PreconditionResult.FromError("Command not found");

			if (HasPermissions(cmd, gUser, permissions))
				return PreconditionResult.FromSuccess();

			if (!permissions.HasPermissionsForKey(cmd))
				return PreconditionResult.FromError($"No permissions setup for the command `{cmd}` but required to use it.");
			else
				return PreconditionResult.FromError("");
		}
	}
}
