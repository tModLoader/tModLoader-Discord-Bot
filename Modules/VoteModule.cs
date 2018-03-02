using System;
using System.Linq;
using System.Threading.Tasks;
using dtMLBot.Configs;
using dtMLBot.Preconditions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace dtMLBot.Modules
{
	[Group("vote")]
	public class VoteModule : ConfigModuleBase<SocketCommandContext>
	{
		public VoteModule(CommandService commandService) : base(commandService)
		{
		}

		[Group("mod")]
		[Alias("-m")]
		[HasPermission]
		public class ModModule : ConfigModuleBase<SocketCommandContext>
		{
			public ModModule(CommandService commandService) : base(commandService)
			{
			}

			[Command("set")]
			[Alias("-s")]
			public async Task SetIdWeightAsync(IUser user, uint weight)
				=> await SetIdWeightAsync(user.Id, weight);

			[Command("set")]
			[Alias("-s")]
			public async Task SetIdWeightAsync(IRole role, uint weight)
				=> await SetIdWeightAsync(role.Id, weight);

			private async Task SetIdWeightAsync(ulong id, uint weight)
			{
				if (!Config.HasVoteDeleteIdWeight(id))
					Config.VoteDeleteWeights.Add(id, weight);
				else
					Config.VoteDeleteWeights[id] = weight;

				if (weight == 1)
					Config.VoteDeleteWeights.Remove(id);

				await Config.Update();
				await ReplyAsync($"Weight for {id} is now {weight}.");
			}

			[Command("delete")]
			[Alias("-d")]
			public async Task DeleteReqIdAsync(IUser user)
				=> await DeleteReqIdAsync(user.Id);

			[Command("delete")]
			[Alias("-d")]
			public async Task DeleteReqIdAsync(IRole role)
				=> await DeleteReqIdAsync(role.Id);

			private async Task DeleteReqIdAsync(ulong id)
			{
				if (!Config.HasVoteDeleteReqId(id))
				{
					await ReplyAsync($"{id} was not marked required.");
					return;
				}

				Config.VoteDeleteReqIds.Remove(id);
				await Config.Update();
			}

			[Command("add")]
			[Alias("-a")]
			public async Task AddReqIdAsync(IUser user)
				=> await AddReqIdAsync(user.Id);

			[Command("add")]
			[Alias("-a")]
			public async Task AddReqIdAsync(IRole role)
				=> await AddReqIdAsync(role.Id);

			private async Task AddReqIdAsync(ulong id)
			{
				if (Config.HasVoteDeleteReqId(id))
				{
					await ReplyAsync($"{id} is already marked required.");
					return;
				}

				Config.VoteDeleteReqIds.Add(id);
				await Config.Update();
			}
		}

		[Command("affirm")]
		[Alias("-a")]
		[HasPermission]
		public async Task AffirmAsync(ulong messageId)
		{
			var msg = await Context.Channel.GetMessageAsync(messageId);

			if (Config.IsVoteDeleteImmune(msg.Author.Id)
				|| msg.Author is SocketGuildUser gu && gu.Roles.Any(x => Config.IsVoteDeleteImmune(x.Id)))
			{
				await ReplyAsync($"Unable to affirm for `{messageId}`, owner is marked immune.");
				return;
			};

			if (msg is IUserMessage message)
			{
				Program.VotesForRemoval.Remove(message.Id);
				await message.DeleteAsync(new RequestOptions { AuditLogReason = "Message was voted to be deleted" });
				//await ReplyAsync($"Message by {message.Author.Username}#{message.Author.Discriminator} was terminated by affirmation.");
			}
		}

		[Command("delete")]
		[Alias("-d")]
		public async Task VoteDeleteAsync(ulong messageId)
		{
			var msg = await Context.Channel.GetMessageAsync(messageId);

			if (Config.IsVoteDeleteImmune(msg.Author.Id)
				|| msg.Author is SocketGuildUser gu && gu.Roles.Any(x => Config.IsVoteDeleteImmune(x.Id)))
			{
				await ReplyAsync($"Unable to start a vote for `{messageId}`, owner is marked immune.");
				return;
			}

			if (msg is IUserMessage message)
				await message.AddReactionAsync(new Emoji("⛔"));
		}
	}
}
