using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Services
{
	public class ModService : BaseService
	{
		public ModService(IServiceProvider services) : base(services)
		{
		}
		
		internal const string WidgetUrl = "https://tml-card.le0n.dev/?v=1.4&modname=";
		//private const string ModInfoUrl = "https://tmlapis.le0n.dev/1.4/mod/";
		//private const string ModListUrl = "https://tmlapis.le0n.dev/1.4/list/";
		
		private static string ModDir => "mods/1.4";
		private static Timer _updateTimer;
		private static SemaphoreSlim _semaphore;
		
		public static string ModPath(string modname) =>
			Path.Combine(ModDir, $"{modname}.json");

		private static List<string> _mods; // Need to cache more data probably.
		public static IEnumerable<string> Mods => _mods ?? (_mods = 
			Directory.GetFiles(ModDir, "*.json")
				.Select(x => Path.GetFileNameWithoutExtension(x).RemoveWhitespace()).ToList());

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
			try
			{
				await Log("Starting 1.4 maintenance");
				// Create dirs
				Directory.CreateDirectory(ModDir);

				string path = Path.Combine(ModDir, "date.txt");
				var dateDiff = TimeSpan.MinValue;
				DateTime todayUTC = DateTime.Now.ToUniversalTime();
				DateTime savedBinaryDate = todayUTC.AddDays(-1);

				// Data.txt present, read
				if (File.Exists(path))
				{
					string savedBinary = await FileUtils.FileReadToEndAsync(_semaphore, path);
					long parsedBinary = long.Parse(savedBinary);
					savedBinaryDate = BotUtils.DateTimeFromUnixTimestampSeconds(parsedBinary);
					dateDiff = BotUtils.DateTimeFromUnixTimestampSeconds(BotUtils.GetCurrentUnixTimestampSeconds()) - savedBinaryDate;
					await Log($"Read date difference for 1.4 mod cache update: {dateDiff}");
				}

				// Needs to maintain data
				//if (dateDiff == TimeSpan.MinValue || dateDiff.TotalHours > 5.99d)
				if (todayUTC.Date != savedBinaryDate.Date)
				{
					//await Log($"Maintenance determined: over 6 hours. Updating...");
					await Log($"Maintenance determined: new day. Updating...");

					string data = await DownloadModListData();
					var modList = JArray.Parse(data);

					var mods = modList.Children().Select(j => new ModInfo((JObject)j, true)).ToArray();

					/*foreach (var jToken in modList)
					{
						string name = jToken["internal_name"]?.ToObject<string>().RemoveWhitespace();
						string jsonPath = Path.Combine(ModDir, $"{name}.json");
						await FileUtils.FileWriteAsync(_semaphore, jsonPath, jToken.ToString(Formatting.Indented));
					}*/

					foreach (var mod in mods)
					{
						string name = mod.InternalName;
						string jsonPath = Path.Combine(ModDir, $"{name}.json");
						await FileUtils.FileWriteAsync(_semaphore, jsonPath, mod.Json.ToString()); // JsonConvert.SerializeObject(mod, Formatting.Indented)
					}

					await FileUtils.FileWriteAsync(_semaphore, path, BotUtils.GetCurrentUnixTimestampSeconds().ToString());
					await Log($"File write successful");
				}
			}
			catch (Exception)
			{
				await Log($"ModService Maintenance failed");
			}
		}

		/*
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
		*/
		
		private async Task<string> DownloadModListData()
		{
			string inputJson;
			await _loggingService.Log(new LogMessage(LogSeverity.Info, nameof(ModService),
					$"Downloading json from Steam..."));

			List<JObject> list = new List<JObject>();

			using var webClient = new WebClient();

			string cursor = "*";
			while (true)
			{
				//Console.WriteLine($"Cursor is {cursor}");

				// https://steamcommunity.com/dev/apikey
				string steamWebAPIKey = Environment.GetEnvironmentVariable("SteamWebAPIKey");

				string steamAPIURL = string.Format("https://api.steampowered.com/IPublishedFileService/QueryFiles/v1?key={0}&appid={1}&cursor={2}&numperpage=10000&cache_max_age_seconds=0&return_details=true&return_kv_tags=true&return_children=true&return_tags=true&return_vote_data=true", steamWebAPIKey, "1281930", HttpUtility.UrlEncode(cursor));

				string response = webClient.DownloadString(steamAPIURL);
				var responseObject = JObject.Parse(response)["response"];
				var publishedfiledetails = responseObject["publishedfiledetails"];
				if (publishedfiledetails == null || publishedfiledetails.Count() == 0)
				{
					await _loggingService.Log(new LogMessage(LogSeverity.Info, nameof(ModService),
					$"No more."));
					break;
				}
				cursor = (string)responseObject["next_cursor"];
				int total = (int)responseObject["total"];

				var modObjects = publishedfiledetails.Children().Cast<JObject>().ToArray();
				list.AddRange(modObjects);
				await _loggingService.Log(new LogMessage(LogSeverity.Info, nameof(ModService),
					$"Downloaded {list.Count} of {total}"));
			}
			await _loggingService.Log(new LogMessage(LogSeverity.Info, nameof(ModService),
					$"Done"));
			inputJson = JsonConvert.SerializeObject(list);
			return inputJson;

			/*
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
			*/
		}
		
		/*
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
		*/
		
		private async Task Log(string msg)
		{
			await _loggingService.Log(new LogMessage(LogSeverity.Info, "ModService", msg));
		}
	}
}