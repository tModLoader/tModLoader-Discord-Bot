using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using tModloaderDiscordBot.Configs;

namespace tModloaderDiscordBot.Services
{
	public class GuildConfigServiceSettings
	{
		public string BaseDir { get; }

		private readonly string _dataDir;
		public string DataDir => Path.Combine(BaseDir, _dataDir);

		public GuildConfigServiceSettings(string baseDir = "", string dataDir = "")
		{
			BaseDir = baseDir;
			_dataDir = dataDir;
		}

		public string GuildPath(ulong guildId) => Path.Combine(DataDir, guildId.ToString());
		public string GuildConfigPath(ulong guildId) => Path.Combine(GuildPath(guildId), "config.json");
		public bool GuildConfigExists(ulong guildId) => File.Exists(GuildConfigPath(guildId));
	}

	public class GuildConfigService
	{
		private readonly SemaphoreSlim _semaphore;
		private readonly CommandService _commandService;
		private readonly DiscordSocketClient _client;
		private readonly IServiceProvider _services;
		internal readonly GuildConfigServiceSettings _config;
		private readonly IDictionary<ulong, GuildConfig> _guildConfigs;

		public GuildConfig GetGuildConfig(ulong id)
		{
			if (_guildConfigs.ContainsKey(id)) return _guildConfigs[id];
			return null;
		}

		public GuildConfigService(IServiceProvider services)
		{
			_commandService = services.GetRequiredService<CommandService>();
			_client = services.GetRequiredService<DiscordSocketClient>();
			_services = services;
			// using semaphore to thread lock as it allows await in asynchronous context
			_semaphore = new SemaphoreSlim(1, 1);
			_config = new GuildConfigServiceSettings(dataDir: "data");
			_guildConfigs = new Dictionary<ulong, GuildConfig>();
		}

		public async Task SetupAsync()
		{
			// iterate guilds and create new configs for them

			foreach (var guild in _client.Guilds.Where(x => !_config.GuildConfigExists(x.Id)))
			{
				Directory.CreateDirectory(_config.GuildPath(guild.Id));
				GuildConfig gConfig = new GuildConfig(guild);
				await WriteGuildConfig(gConfig);
			}

			await UpdateCache();
		}

		internal Task<bool> UpdateCacheForGuild(GuildConfig config)
		{
			if (_guildConfigs.ContainsKey(config.GuildId))
			{
				_guildConfigs[config.GuildId] = config;
				return Task.FromResult(true);
			}

			return Task.FromResult(_guildConfigs.TryAdd(config.GuildId, config));
		}

		internal async Task UpdateCache()
		{
			var filePaths = Directory.GetFiles(_config.DataDir, "config.json", SearchOption.AllDirectories);

			foreach (var filePath in filePaths)
			{
				await _semaphore.WaitAsync();
				string json;
				try
				{
					json = await File.ReadAllTextAsync(filePath);
				}
				finally
				{
					_semaphore.Release();
				}

				var config = JsonConvert.DeserializeObject<GuildConfig>(json);
				await UpdateCacheForGuild(config);
			}
		}

		internal async Task<GuildConfig> ReadGuildConfig(ulong guildId)
		{
			await _semaphore.WaitAsync();
			string json;
			try
			{
				json = await File.ReadAllTextAsync(_config.GuildConfigPath(guildId));
			}
			finally
			{
				_semaphore.Release();
			}

			var config = JsonConvert.DeserializeObject<GuildConfig>(json);
			return config;
		}

		internal async Task WriteGuildConfig(GuildConfig config)
		{
			Directory.CreateDirectory(_config.GuildPath(config.GuildId));
			var json = JsonConvert.SerializeObject(config, Formatting.Indented);
			await _semaphore.WaitAsync();
			try
			{
				await File.WriteAllTextAsync(_config.GuildConfigPath(config.GuildId), json);
			}
			finally
			{
				_semaphore.Release();
			}
		}
	}
}
