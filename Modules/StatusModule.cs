using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using dtMLBot.Configs;
using dtMLBot.Preconditions;
using Discord.Commands;

namespace dtMLBot.Modules
{
	[Group("status")]
	public class StatusModule : ConfigModuleBase<SocketCommandContext>
	{
		public StatusModule(CommandService commandService) : base(commandService)
		{
		}

		[Command("remove")]
		[Alias("delete", "-d")]
		[HasPermission]
		public async Task RemoveAsync(params string[] args)
		{
			foreach (var noa in args)
			{
				var name = noa.ToLowerInvariant();
				var address = noa;
				var msg = await ReplyAsync($"Validating address...");

				if (Config.HasAddressName(name))
				{
					Config.StatusAddresses.Remove(name);
					await Config.Update();
					await msg.ModifyAsync(x => x.Content = $"Address for `{name}` was removed.");
					continue;
				}

				CheckUriPrefix(ref address);

				if (Config.HasAdresses(address))
				{
					foreach (var addr in Config.StatusAddresses.Where(x => x.Value.EqualsIgnoreCase(address)))
						Config.StatusAddresses.Remove(addr.Key);

					await Config.Update();
					await msg.ModifyAsync(x => x.Content = $"Address `{address}` was removed.");
					continue;
				}

				await msg.ModifyAsync(x => x.Content = $"Address `{address}` not found.");
			}
		}

		[Command("add")]
		[Alias("-a")]
		[HasPermission]
		public async Task AddAsync(string nameParam, string addrParam)
		{
			var msg = await ReplyAsync($"Validating address...");

			var name = nameParam.ToLowerInvariant();
			var addr = addrParam;/* addrParam.ToLowerInvariant();*/
			CheckUriPrefix(ref addr);
			var isLegit = IsUriLegit(addr, out var uri);

			if (!isLegit)
			{
				await msg.ModifyAsync(x => x.Content = $"Address `{addr}` is not a valid web address.");
				return;
			}

			if (Config.HasAdresses(uri.AbsoluteUri))
			{
				await msg.ModifyAsync(x => x.Content = $"Address `{addr}` is already present.");
				return;
			}

			if (Config.HasAddressName(name))
			{
				await msg.ModifyAsync(x => x.Content = $"Address for `{name}` already exists.");
				return;
			}

			Config.StatusAddresses.Add(name, addr);
			await Config.Update();
			await msg.ModifyAsync(x => x.Content = $"Address `{addr}` was added under name `{name}`.");
		}

		[Command]
		[Priority(-99)]
		public async Task Default([Remainder]string toCheckParam = "")
		{
			var msg = await Context.Channel.SendMessageAsync("Performing status checks...");

			try
			{
				var sb = new StringBuilder();

				var toCheck = toCheckParam.ToLowerInvariant();
				if (toCheck.Length > 0)
				{
					// TODO levenhstein dist, closest guess
					if (!Config.HasAddressName(toCheck))
					{
						await msg.ModifyAsync(x => x.Content = $"Address for `{toCheck}` was not found");
						return;
					}

					var addr = Config.StatusAddresses[toCheck];
					await msg.ModifyAsync(x => x.Content = $"{toCheck}: `{PingResultString(addr)}` ({addr})");
					return;
				}

				if (Config.StatusAddresses.Count <= 0)
				{
					await msg.ModifyAsync(x =>
						x.Content = "No addresses to check.");
					return;
				}

				foreach (var addr in Config.StatusAddresses)
					sb.AppendLine($"{addr.Key}: `{PingResultString(addr.Value)}` ({addr.Value})");

				//sb.AppendLine($"Discord: `{PingSuccessString("https://status.discordapp.com/")}`");
				//sb.AppendLine($"Mod Browser: `{PingSuccessString("http://javid.ddns.net/tModLoader/")}`");
				//sb.AppendLine($"Website: `{PingSuccessString("https://tmodloader.net/")}`");

				await msg.ModifyAsync(x => x.Content = sb.ToString());
			}
			catch (Exception)
			{
				// Discard PingExceptions and return false;
				await msg.ModifyAsync(x => x.Content =
					"Something went wrong when trying to check status.");
			}
		}

		private static bool IsUriLegit(string addr, out Uri uri)
		{
			return Uri.TryCreate(addr, UriKind.Absolute, out uri)
				&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
		}

		private static void CheckUriPrefix(ref string addr)
		{
			if (addr.StartsWith("www."))
				addr = addr.Substring(3);

			if (!addr.StartsWith("http://") && !addr.StartsWith("https://"))
				addr = $"http://{addr}";
		}

		private static bool Ping(string addr)
		{
			var request = WebRequest.Create(addr);
			var response = (HttpWebResponse)request.GetResponse();
			return response != null && response.StatusCode == HttpStatusCode.OK;
		}

		private static string PingResultString(string addr)
		{
			var result = IsUriLegit(addr, out var _);

			if (!result)
				return "Invalid address";

			try
			{
				return Ping(addr)
					? "Online (Response OK)"
					: "Offline (Response FAIL)";
			}
			catch (Exception)
			{
				return "Invalid address";
			}
		}
	}
}
