using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using tModloaderDiscordBot.Preconditions;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Modules
{
	[Group("status")]
	public class StatusModule : BotModuleBase
	{
		public SiteStatusService StatusService { get; set; }

		protected override void BeforeExecute(CommandInfo command)
		{
			base.BeforeExecute(command);
			StatusService.SetGID(Context.Guild.Id);
		}

		[Command("remove")]
		[Alias("delete", "-d")]
		[HasPermission]
		[Priority(1)]
		public async Task RemoveAsync(params string[] args)
		{
			foreach (var noa in args)
			{
				var name = noa.ToLowerInvariant();
				var address = noa;
				var msg = await ReplyAsync($"Validating address...");

				if (StatusService.HasName(name))
				{
					Config.SiteStatuses.Remove(Config.SiteStatuses.First(x => x.Name.EqualsIgnoreCase(name)));
					await Config.Update(GuildConfigService);
					await StatusService.UpdateForConfig(Config);
					await msg.ModifyAsync(x => x.Content = $"Address for `{name}` was removed.");
					continue;
				}

				SiteStatus.CheckUriPrefix(ref address);

				if (StatusService.HasAddress(address))
				{
					Config.SiteStatuses = Config.SiteStatuses.Where(x => !x.Name.EqualsIgnoreCase(address)).ToList();
					await Config.Update(GuildConfigService);
					await StatusService.UpdateForConfig(Config);
					await msg.ModifyAsync(x => x.Content = $"Address `{address}` was removed.");
					continue;
				}

				await msg.ModifyAsync(x => x.Content = $"Address `{address}` not found.");
			}
		}

		[Command("add")]
		[Alias("-a")]
		[HasPermission]
		[Priority(1)]
		public async Task AddAsync(string nameParam, string addrParam)
		{
			var msg = await ReplyAsync($"Validating address...");

			var name = nameParam.ToLowerInvariant();
			var addr = addrParam;/* addrParam.ToLowerInvariant();*/
			SiteStatus.CheckUriPrefix(ref addr);
			var isLegit = SiteStatus.IsUriLegit(addr, out var uri);

			if (!isLegit)
			{
				await msg.ModifyAsync(x => x.Content = $"Address `{addr}` is not a valid web address.");
				return;
			}

			if (StatusService.HasAddress(uri.AbsoluteUri))
			{
				await msg.ModifyAsync(x => x.Content = $"Address `{addr}` is already present.");
				return;
			}

			if (StatusService.HasAddress(name))
			{
				await msg.ModifyAsync(x => x.Content = $"Address for `{name}` already exists.");
				return;
			}

			Config.SiteStatuses.Add(new SiteStatus { Address = addr, Name = name });
			await Config.Update(GuildConfigService);
			await StatusService.UpdateForConfig(Config);
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
					if (!StatusService.HasAddress(toCheck))
					{
						await msg.ModifyAsync(x => x.Content = $"Address for `{toCheck}` was not found");
						return;
					}

					var cachedResult = StatusService.GetCachedResult(toCheck);
					if (!cachedResult.IsDefault())
						await msg.ModifyAsync(x => x.Content = string.Format("{0} {1} {2}", (toCheck + ":"), ("`" + cachedResult.cachedResult + "`"), ("(" + cachedResult.url + ")")));
					else
						await msg.ModifyAsync(x => x.Content = "Something went wrong.");
					return;
				}

				if (Config.SiteStatuses.Count <= 0)
				{
					await msg.ModifyAsync(x => x.Content = "No addresses to check.");
					return;
				}

				foreach (var status in StatusService.AllSiteStatuses())
				{
					var editString = string.Format("{0} {1} {2}", (status.Name + ":"), ("`" + status.CachedResult + "`"), ("(" + status.Address + ")"));
					sb.AppendLine(editString);
				}

				await msg.ModifyAsync(x => x.Content = sb.ToString());
			}
			catch (Exception)
			{
				// Discard PingExceptions and return false;
				await msg.ModifyAsync(x => x.Content = "Something went wrong when trying to check status.");
			}
		}
	}
}
