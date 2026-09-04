using System;
using Discord.Commands;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Modules;
using tModloaderDiscordBot.Services;

/// <summary>
/// Defines the base starting point of any command requiring access to the Guild's configuration
/// </summary>
public abstract class ConfigModuleBase : BotModuleBase
{
    public GuildConfigService GuildConfigService { get; set; }

    /// <summary>
    /// The Guild's configuration which the command was executed in
    /// </summary>
    [DontInject] public GuildConfig Config { get; set; }

    // Note: Context is set before execute, not available in constructor
    protected override void BeforeExecute(CommandInfo command)
    {
        base.BeforeExecute(command);

        if (GuildConfigService == null)
            throw new Exception("Failed to get guild config service");

        Config = GuildConfigService.GetConfig(Context.Guild.Id);
        if (Config == null)
            throw new Exception("Failed to get guild config");

        Config.Initialize(GuildConfigService);
    }
}