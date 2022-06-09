using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace tModloaderDiscordBot.Services
{
	public class RecruitmentChannelService : BaseService
	{
#if TESTBOT
		private const ulong recruitmentChannelId = 556634834093473793;
#else
		private const ulong recruitmentChannelId = 693622571018485830;
#endif

		internal class TrackedMessage
		{
			internal ulong originalAuthor;
			internal DateTimeOffset lastRefresh;

			public TrackedMessage(ulong originalAuthor, DateTimeOffset lastRefresh)
			{
				this.originalAuthor = originalAuthor;
				this.lastRefresh = lastRefresh;
			}
		}

		private readonly DiscordSocketClient _client;

		internal ITextChannel recruitmentChannel;
		internal static readonly Dictionary<ulong, TrackedMessage> TrackedMessages = new Dictionary<ulong, TrackedMessage>();

		public RecruitmentChannelService(IServiceProvider services) : base(services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();
			_client.ReactionAdded += HandleReactionAdded;
		}

		internal async Task SetupAsync()
		{
			recruitmentChannel = (ITextChannel)_client.GetChannel(recruitmentChannelId);

			await _loggingService.Log(new LogMessage(LogSeverity.Info, "Recruitment", $"Looking for Recruitment channel"));
			if (recruitmentChannel != null)
			{
				await _loggingService.Log(new LogMessage(LogSeverity.Info, "Recruitment", $"Recruitment channel found"));

				var messages = await recruitmentChannel.GetMessagesAsync(100).FlattenAsync(); //defualt is 100
				messages = messages.Where(x => !x.IsPinned);

				var oldMessages = messages.Where(x => DateTimeOffset.UtcNow > x.Timestamp.AddDays(30)); // is this a good number?
				foreach (var oldMessage in oldMessages)
					await oldMessage.DeleteAsync();
				await _loggingService.Log(new LogMessage(LogSeverity.Info, "Recruitment", $"{oldMessages.Count()} old messages cleared from #recruitment"));

				var recentMessages = messages.Except(oldMessages);
				foreach (var recentMessage in recentMessages)
				{
					var embed = recentMessage.Embeds.FirstOrDefault();
					if (embed == null || !embed.Author.HasValue)
						continue;

					var embedAuthor = embed.Author.Value.Name;

					string[] embedAuthorParts = embedAuthor.Split('#', 2);
					if (embedAuthorParts.Length != 2)
						continue;

					var originalUser = _client.GetUser(embedAuthorParts[0], embedAuthorParts[1]);
					if(originalUser != null)
						TrackedMessages[recentMessage.Id] = new TrackedMessage(originalUser.Id, recentMessage.Timestamp);
				}
				await _loggingService.Log(new LogMessage(LogSeverity.Info, "Recruitment", $"{TrackedMessages.Count()} messages restored from #recruitment "));

				/*
				var oldMessages = messages.Where(x => DateTimeOffset.UtcNow > x.Timestamp.AddDays(13)); // 2 week limit 
				var recentMessages = messages.Except(oldMessages);

				await recruitmentChannel.DeleteMessagesAsync(recentMessages);
				foreach (var oldMessage in oldMessages)
				{
					await oldMessage.DeleteAsync();
				}
				*/
			}
		}
		
		private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> cacheableMessage, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
		{
			if (recruitmentChannel == null)
				return;

			if (!reaction.User.IsSpecified || reaction.User.Value.IsBot || reaction.User.Value.IsWebhook)
				return;

			string emoteName = reaction.Emote.Name;
			if (emoteName != "📡" && emoteName != "❌") 
				return;

			if (!(reaction.User.Value is IGuildUser reactionAuthor))
				return;

			if (!channel.HasValue)
				await channel.DownloadAsync();

			// Users reacting to messages they own in recruitmentChannel can bump their message.
			if (channel.Id == recruitmentChannel.Id)
			{
				// Thought: We could just track original message, and update it from that, rather than delete. Allows original user to update.

				var message = await cacheableMessage.GetOrDownloadAsync();

				if (!TrackedMessages.TryGetValue(message.Id, out TrackedMessage trackedMessage))
					return;

				if (reactionAuthor.Id != trackedMessage.originalAuthor)
					return;

				if (emoteName == "❌")
				{
					await message.DeleteAsync();
					TrackedMessages.Remove(message.Id);
					return;
				}
				
				// Check time to throttle bumping interval
				if (trackedMessage.lastRefresh.AddDays(1) > DateTimeOffset.UtcNow)
				{
					await message.AddReactionAsync(new Emoji("🛑"));
					return;
				}

				var embed = message.Embeds.FirstOrDefault();
				if (embed == null || !embed.Author.HasValue)
					return;

				await message.DeleteAsync();
				TrackedMessages.Remove(message.Id);

				var botMessage = await recruitmentChannel.SendMessageAsync("", embed: (Embed)embed); // embed will maintain timestamp.
				await botMessage.AddReactionAsync(new Emoji("📡"));

				TrackedMessages[botMessage.Id] = new TrackedMessage(trackedMessage.originalAuthor, botMessage.Timestamp);
			}
			else // Moderators reacting to messages in other channels can move message to Recruitment
			{
				var authorPermissionsInRectuitmentChannel = reactionAuthor.GetPermissions((IGuildChannel)recruitmentChannel);
				if (!authorPermissionsInRectuitmentChannel.SendMessages)
					return; // Not authorized to post in recruitment channel.

				var message = await cacheableMessage.GetOrDownloadAsync();

				string contents = message.Content;
				await message.DeleteAsync();

				//var botMessage = await recruitmentChannel.SendMessageAsync($"Recruitment Message from {message.Author.Mention}>:\n{contents}"); //{message.Author.Mention}
				//await botMessage.AddReactionAsync(new Emoji("📡"));

				var embed = new EmbedBuilder()
					.WithAuthor(message.Author)
					.WithColor(Color.Blue)
					.WithDescription(contents)
					.WithCurrentTimestamp()
					.Build();
				var botMessage = await recruitmentChannel.SendMessageAsync("", embed: embed);
				await botMessage.AddReactionAsync(new Emoji("📡"));
				await botMessage.AddReactionAsync(new Emoji("❌"));

				TrackedMessages[botMessage.Id] = new TrackedMessage(message.Author.Id, botMessage.Timestamp);
			}
			return;
		}
	}
}
