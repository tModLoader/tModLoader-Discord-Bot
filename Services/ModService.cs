using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Services
{
	public class ModService : BaseService
	{
		internal const string QueryDownloadUrl = "http://javid.ddns.net/tModLoader/tools/querymoddownloadurl.php?modname=";
		internal const string QueryHomepageUrl = "http://javid.ddns.net/tModLoader/tools/querymodhomepage.php?modname=";
		internal const string WidgetUrl = "http://javid.ddns.net/tModLoader/widget/widgetimage/";
		internal const string XmlUrl = "http://javid.ddns.net/tModLoader/listmods.php";
		internal const string ModInfoUrl = "http://javid.ddns.net/tModLoader/tools/modinfo.php";
		internal const string HomepageUrl = "http://javid.ddns.net/tModLoader/moddescription.php";
		internal const string PopularUrl = "http://javid.ddns.net/tModLoader/tools/populartop10.php";
		internal const string HotUrl = "http://javid.ddns.net/tModLoader/tools/hottop10.php";
		internal const string NewestReleaseUrl = "https://api.github.com/repos/tModLoader/tModLoader/releases/latest";

		private static string ModDir => "mods";
		internal static string tMLVersion;

		public static string ModPath(string modname) =>
			Path.Combine(ModDir, $"{modname}.json");

		public static IEnumerable<string> Mods =>
			Directory.GetFiles(ModDir, "*.json")
				.Select(x => Path.GetFileNameWithoutExtension(x).RemoveWhitespace());

		private static SemaphoreSlim _semaphore;
		//private static Timer _updateTimer;
		private readonly DiscordSocketClient _client;

		public ModService(IServiceProvider services, DiscordSocketClient client) : base(services)
		{
			_client = client;
		}

		public async Task Initialize()
		{
			using var client = new WebClient();
			//The Github api expects at least more than 5 letters here, change it to whatever you want
			client.Headers.Add("user-agent", "Discord.Net");
			
			tMLVersion = $"tModLoader {JObject.Parse(client.DownloadString(NewestReleaseUrl)).GetValue("tag_name")}";
			_semaphore = new SemaphoreSlim(1, 1);

			//if (_updateTimer == null)
			//{
			//	_updateTimer = new Timer(async (e) =>
			//	{
			//		await Log("Running maintenance from 6 hour timer");
			//		await Maintain(_client);
			//	},
			//	null, TimeSpan.FromHours(6), TimeSpan.FromHours(6));
			//}
		}

		public async Task Maintain(IDiscordClient client)
		{
			await Log("Starting maintenance");
			// Create dirs
			Directory.CreateDirectory(ModDir);

			var path = Path.Combine(ModDir, "date.txt");
			var dateDiff = TimeSpan.MinValue;

			// Data.txt present, read
			if (File.Exists(path))
			{
				var savedBinary = await FileUtils.FileReadToEndAsync(_semaphore, path);
				var parsedBinary = long.Parse(savedBinary);
				var savedBinaryDate = BotUtils.DateTimeFromUnixTimestampSeconds(parsedBinary);
				dateDiff = BotUtils.DateTimeFromUnixTimestampSeconds(BotUtils.GetCurrentUnixTimestampSeconds()) - savedBinaryDate;
				await Log($"Read date difference for mod cache update: {dateDiff}");
			}

			// Needs to maintain data
			if (dateDiff == TimeSpan.MinValue
				|| dateDiff.TotalHours > 5.99d)
			{
				await Log($"Maintenance determined: over 6 hours. Updating...");
				var data = await DownloadData();
				JObject jsonObject = JObject.Parse(data);
				JArray modlist;
				string modlist_compressed = (string)jsonObject["modlist_compressed"];
				if (modlist_compressed != null)
				{
					byte[] compressedData = Convert.FromBase64String(modlist_compressed);
					using (GZipStream zip = new GZipStream(new MemoryStream(compressedData), CompressionMode.Decompress))
					using (var reader = new StreamReader(zip))
						modlist = JArray.Parse(reader.ReadToEnd());
				}
				else
				{
					// Fallback if needed.
					modlist = (JArray)jsonObject["modlist"];
				}
				foreach (var jtoken in modlist)
				{
					var name = jtoken.SelectToken("name").ToObject<string>().RemoveWhitespace();
					if(jtoken["download"] == null)
						jtoken["download"] = $"http://javid.ddns.net/tModLoader/download.php?Down=mods/{name}.tmod";
					if (jtoken["modside"] == null)
						jtoken["modside"] = "Both";
					var jsonPath = Path.Combine(ModDir, $"{name}.json");
					await FileUtils.FileWriteAsync(_semaphore, jsonPath, jtoken.ToString(Formatting.Indented));
				}

				await FileUtils.FileWriteAsync(_semaphore, path, BotUtils.GetCurrentUnixTimestampSeconds().ToString());
				await Log($"File write successful");
			}
		}

		public async Task<bool> TryCacheMod(string name)
		{
			await Log($"Attempting to update cache for mod {name}");
			var data = await DownloadSingleData(name);
			if (data.StartsWith("Failed:"))
			{
				await Log($"Cache update for mod {name} failed");
				return false;
			}

			var mod = JObject.Parse(data);
			await FileUtils.FileWriteAsync(_semaphore, Path.Combine(ModDir, $"{mod.SelectToken("name").ToObject<string>()}.json"), mod.ToString(Formatting.Indented));
			await Log($"Cache update for mod {name} succeeded");
			return true;
		}

		private async Task<string> DownloadData()
		{
			await Log($"Requesting DownloadData. tMod version: {tMLVersion}");
			using (var client = new System.Net.Http.HttpClient())
			{
				//var version = await GetTMLVersion();

				var values = new Dictionary<string, string>
				{
					{ "modloaderversion", $"tModLoader {tMLVersion}" },
					{ "platform", "w"}
				};
				var content = new System.Net.Http.FormUrlEncodedContent(values);

				await Log("Sending post request");
				var response = await client.PostAsync(XmlUrl, content);

				await Log("Reading post request");
				var postResponse = await response.Content.ReadAsStringAsync();

				await Log("Done downloading data");
				return postResponse;
			}
		}

		private async Task<string> DownloadSingleData(string name)
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var response = await client.GetAsync(ModInfoUrl + $"?modname={name}");
				var postResponse = await response.Content.ReadAsStringAsync();
				return postResponse;
			}
		}

		private async Task Log(string msg)
		{
			await _loggingService.Log(new LogMessage(LogSeverity.Info, "ModService", msg));
		}
	}
}
