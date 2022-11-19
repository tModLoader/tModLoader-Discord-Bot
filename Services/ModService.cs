using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Services
{
	public class ModService : BaseService
	{
		public ModService(IServiceProvider services) : base(services)
		{
		}
		
		internal const string WidgetUrl = "https://tml-readme-card.repl.co/?v=1.4&modname=";
		private const string ModInfoUrl = "https://tmlapis.tomat.dev/1.4/mod/";
		private const string ModListUrl = "https://tmlapis.tomat.dev/1.4/list/";
		
		private static string ModDir => "mods/1.4";
		private static Timer _updateTimer;
		private static SemaphoreSlim _semaphore;
		
		public static string ModPath(string modname) =>
			Path.Combine(ModDir, $"{modname}.json");
		
		public ModService Initialize()
		{
			_semaphore = new SemaphoreSlim(1, 1);
			_updateTimer ??= new Timer(UpdateMods, null, TimeSpan.FromHours(6), TimeSpan.FromHours(6));
			return this;
		}
		
		private async void UpdateMods(object state)
		{
			await Log("Running 1.4 maintenance from 6 hour timer");
			await Maintain();
		}
		
		public async Task Maintain()
		{
			await Log("Starting 1.4 maintenance");
			// Create dirs
			Directory.CreateDirectory(ModDir);

			string path = Path.Combine(ModDir, "date.txt");
			var dateDiff = TimeSpan.MinValue;

			// Data.txt present, read
			if (File.Exists(path))
			{
				string savedBinary = await FileUtils.FileReadToEndAsync(_semaphore, path);
				long parsedBinary = long.Parse(savedBinary);
				var savedBinaryDate = BotUtils.DateTimeFromUnixTimestampSeconds(parsedBinary);
				dateDiff = BotUtils.DateTimeFromUnixTimestampSeconds(BotUtils.GetCurrentUnixTimestampSeconds()) - savedBinaryDate;
				await Log($"Read date difference for 1.4 mod cache update: {dateDiff}");
			}

			// Needs to maintain data
			if (dateDiff == TimeSpan.MinValue || dateDiff.TotalHours > 5.99d)
			{
				await Log($"Maintenance determined: over 6 hours. Updating...");
				
				string data = await DownloadModListData();
				var modList = JArray.Parse(data);
				
				foreach (var jToken in modList)
				{
					string name = jToken["internal_name"]?.ToObject<string>().RemoveWhitespace();
					string jsonPath = Path.Combine(ModDir, $"{name}.json");
					await FileUtils.FileWriteAsync(_semaphore, jsonPath, jToken.ToString(Formatting.Indented));
				}

				await FileUtils.FileWriteAsync(_semaphore, path, BotUtils.GetCurrentUnixTimestampSeconds().ToString());
				await Log($"File write successful");
			}
		}

		public async Task<bool> TryCacheMod(string name)
		{
			await Log($"Attempting to update cache for mod {name}");
			
			string data = await DownloadSingleData(name);
			if (data is null)
			{
				await Log($"Cache update for mod {name} failed");
				return false;
			}

			var mod = JObject.Parse(data);
			await FileUtils.FileWriteAsync(_semaphore, Path.Combine(ModDir, $"{mod["internal_name"]?.ToObject<string>()}.json"), mod.ToString(Formatting.Indented));
			await Log($"Cache update for mod {name} succeeded");
			return true;
		}
		
		private async Task<string> DownloadModListData()
		{
			using var client = new HttpClient();
			var response = await client.GetAsync(ModListUrl);

			if (!response.IsSuccessStatusCode)
			{
				await _loggingService.Log(new LogMessage(LogSeverity.Error, nameof(ModService),
					$"'{ModInfoUrl}' responded with code {response.StatusCode}"));
				return null;
			}
				
			string postResponse = await response.Content.ReadAsStringAsync();
			return postResponse;
		}
		
		private async Task<string> DownloadSingleData(string name)
		{
			using var client = new HttpClient();
			var response = await client.GetAsync(ModInfoUrl + name);

			if (!response.IsSuccessStatusCode)
			{
				await _loggingService.Log(new LogMessage(LogSeverity.Error, nameof(ModService),
					$"'{ModInfoUrl + name}' responded with code {response.StatusCode}"));
				return null;
			}
				
			string postResponse = await response.Content.ReadAsStringAsync();
			return postResponse;
		}
		
		private async Task Log(string msg)
		{
			await _loggingService.Log(new LogMessage(LogSeverity.Info, "ModService", msg));
		}
	}
}