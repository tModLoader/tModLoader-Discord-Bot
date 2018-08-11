using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using tModloaderDiscordBot.Configs;

namespace tModloaderDiscordBot.Preconditions
{
	internal class HasPermissionAttribute : PreconditionAttribute
	{
		public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			if (context.Guild == null)
				return PreconditionResult.FromError("");

			if (!ConfigManager.IsGuildManaged(context.Guild.Id))
				return PreconditionResult.FromError($"No config found for guild {context.Guild.Id}");

			GuildConfig config;
			if ((config = ConfigManager.GetManagedConfig(context.Guild.Id)) == null)
				return PreconditionResult.FromError("");

			if (context.User.Id == context.Guild.OwnerId)
				return PreconditionResult.FromSuccess();

			var commandService = (CommandService)services.GetService(typeof(CommandService));
			if (commandService == null)
				return PreconditionResult.FromError($"Could not find command service from service provider");

			var hasPerm = false;
			var cmd = await BotUtils.SearchCommand(commandService, context, command.Module.Name);
			if (cmd == null)
				return PreconditionResult.FromError("Module not found");

			// check if has permission by module name
			cmd = $"module:{cmd}";
			hasPerm = config.Permissions.MapHasPermissionsFor(cmd);
			if (!hasPerm)
			{
				cmd = await BotUtils.SearchCommand(commandService, context, command.Name);
				if (cmd == null)
					return PreconditionResult.FromError("Command not found");

				hasPerm = config.Permissions.MapHasPermissionsFor(cmd);
				// check if user has permission for command
				if (!hasPerm) hasPerm = config.Permissions.HasPermission(cmd, context.User.Id);
				if (!hasPerm)
				{
					// check if user has roles that have permission
					if (!(context.User is IGuildUser guildUser))
						return PreconditionResult.FromError("");

					hasPerm = guildUser.RoleIds.Any(roleId => config.Permissions.HasPermission(cmd, roleId));
				}
			}

			if (config.Permissions.IsBlocked(context.User.Id))
				return PreconditionResult.FromError("User is blocked");

			if (!hasPerm)
				return PreconditionResult.FromError($"No permissions setup for the command `{cmd}` but required to use it.");

			return PreconditionResult.FromSuccess();
		}
	}
}
