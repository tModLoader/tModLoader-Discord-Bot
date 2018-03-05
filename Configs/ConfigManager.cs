using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace tModloaderDiscordBot.Configs
{
	public static class ConfigManager
	{
		public static readonly IDictionary<ulong, GuildConfig> Cache = new Dictionary<ulong, GuildConfig>();

		private static bool _initialized;

		//private static Mutex mutex = new Mutex();
		// using semaphore to thread lock as it allows await in asynchronous context
		private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

		private static readonly string BaseDir = AppContext.BaseDirectory;
		private static readonly string DataDir = Path.Combine(BaseDir, "data");
		private static string _guildPath(ulong guildId) => Path.Combine(DataDir, guildId.ToString());
		private static string _guildConfigPath(ulong guildId) => Path.Combine(_guildPath(guildId), "config.json");

		public static async Task Initialize()
		{
			if (!_initialized)
			{
				await SetupHierarchy();
				await CacheData();
			}
			_initialized = true;
		}

		// TODO async code cant have ref or out parameters, c#7 tuple types/literals vs synchronous out parameter?
		/*
		 * var tuple =  ConfigManager.GetManagedConfig(guild.Id);
			if (tuple.Item1)
				return;
			var config = tuple.Item2;
		 */
		//internal static async Task<(bool, GuildConfig)> GetManagedConfig(ulong guildId)
		//{
		//	if (IsGuildManaged(guildId))
		//	{
		//		await UpdateCacheForGuild(guildId);
		//		return (true, Cache[guildId]);
		//	}
		//	return (false, null);
		//}

		/*
		 * if (!ConfigManager.GetManagedConfig(Context.Guild.Id, out var config))
                return;
		 */
		internal static GuildConfig GetManagedConfig(ulong guildId)
		{
			if (!IsGuildManaged(guildId))
				return null;

			// can't async here. no async in ctor
#pragma warning disable 4014
			if (!Cache.ContainsKey(guildId))
				UpdateCacheForGuild(guildId);
#pragma warning restore 4014
			return Cache[guildId];
		}

		internal static bool IsGuildManaged(ulong guildId)
			=> File.Exists(_guildConfigPath(guildId));

		internal static async Task SetupForGuild(ulong guildId)
		{
			var config = new GuildConfig { GuildId = guildId };
			config.ValidataData();
			await UpdateForGuild(config);
		}

		/// <summary>
		/// Will attempt to update cache for a guild
		/// </summary>
		internal static async Task UpdateCacheForGuild(ulong guildId, bool noCheck = false)
		{
			if (noCheck || IsGuildManaged(guildId))
			{
				await Semaphore.WaitAsync();
				string json;
				try
				{
					json = await File.ReadAllTextAsync(_guildConfigPath(guildId));
				}
				finally
				{
					Semaphore.Release();
				}

				var config = JsonConvert.DeserializeObject<GuildConfig>(json);
				await CacheConfigOrUpdate(config);
			}
		}

		private static async Task SetupHierarchy()
		{
			await Task.Run(() =>
			{
				Directory.CreateDirectory(DataDir);
			});
		}

		/// <summary>
		/// Will read, validate and cache all configs
		/// </summary>
		private static async Task CacheData()
		{
			var filePaths = Directory.GetFiles(DataDir, "config.json", SearchOption.AllDirectories);

			foreach (var filePath in filePaths)
			{
				await Semaphore.WaitAsync();
				string json;
				try
				{
					json = await File.ReadAllTextAsync(filePath);
				}
				finally
				{
					Semaphore.Release();
				}

				var config = JsonConvert.DeserializeObject<GuildConfig>(json);
				if (config.ValidataData())
					await CacheConfigOrUpdate(config);
				else
					await UpdateForGuild(config);
			}
		}

		/// <summary>
		/// Will update or put a config in cache
		/// </summary>
		private static async Task CacheConfigOrUpdate(GuildConfig config)
		{
			await Task.Run(() =>
			{
				if (Cache.ContainsKey(config.GuildId))
					Cache[config.GuildId] = config;
				else
					Cache.TryAdd(config.GuildId, config);
			});
		}

		/// <summary>
		/// Will write the guild config to its config file path
		/// Write operation is locked by semaphore
		/// After write, updates cache
		/// </summary>
		internal static async Task UpdateForGuild(GuildConfig config)
		{
			Directory.CreateDirectory(_guildPath(config.GuildId));
			var json = JsonConvert.SerializeObject(config, Formatting.Indented);
			await Semaphore.WaitAsync();
			try
			{
				await File.WriteAllTextAsync(_guildConfigPath(config.GuildId), json);
			}
			finally
			{
				Semaphore.Release();
			}
			await CacheConfigOrUpdate(config);
		}
	}
}
