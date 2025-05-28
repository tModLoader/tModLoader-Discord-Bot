using Discord;

namespace tModloaderDiscordBot;

public static class ProgramExtensions
{
	public static UserStatus ToUserStatus(this ConnectionState state)
	{
		return state switch
		{
			ConnectionState.Connected => UserStatus.Online,
			ConnectionState.Disconnected => UserStatus.DoNotDisturb,
			ConnectionState.Connecting => UserStatus.Idle,
			ConnectionState.Disconnecting => UserStatus.DoNotDisturb,
			_ => UserStatus.Online
		};
	}
}