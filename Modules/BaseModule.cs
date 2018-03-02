using System.Linq;
using System.Threading.Tasks;
using dtMLBot.Configs;
using Discord;
using Discord.Commands;

namespace dtMLBot.Modules
{
	public class BaseModule : ConfigModuleBase<SocketCommandContext>
	{
		public BaseModule(CommandService commandService) : base(commandService)
		{
		}
	}
}
