using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace tModloaderDiscordBot
{
	//public struct Mod
	//{
	//	public readonly string DisplayName;
	//	public readonly string InternalName;
	//	public readonly string Version;
	//	public readonly string Author;
	//	public readonly string DownloadUrl;
	//	public readonly string Downloads;
	//	public readonly string Hot;
	//	public readonly string UpdateTimeStamp;
	//	public readonly string ModReferences;
	//	public readonly string ModSide;

	//	public Mod(string displayName, string internalName, string version, string author, string downloadUrl, string downloads, string hot, string updateTimeStamp, string modReferences, string modSide)
	//	{
	//		DisplayName = displayName;
	//		InternalName = internalName;
	//		Version = version;
	//		Author = author;
	//		DownloadUrl = downloadUrl;
	//		Downloads = downloads;
	//		Hot = hot;
	//		UpdateTimeStamp = updateTimeStamp;
	//		ModReferences = modReferences;
	//		ModSide = modSide;
	//	}
	//}

	// TODO clean this shit. (ported 1 year old code)
	public static class ModsManager
	{
		//public static IList<Mod> Mods;

		// old fashion, synchronous
		//internal static object Locker = new object();
		private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
		internal const string QueryDownloadUrl = "http://javid.ddns.net/tModLoader/tools/querymoddownloadurl.php?modname=";
		internal const string QueryHomepageUrl = "http://javid.ddns.net/tModLoader/tools/querymodhomepage.php?modname=";
		internal const string WidgetUrl = "http://javid.ddns.net/tModLoader/widget/widgetimage/";
		internal const string XmlUrl = "http://javid.ddns.net/tModLoader/listmods.php";
		internal const string ModInfoUrl = "http://javid.ddns.net/tModLoader/tools/modinfo.php";
		internal const string HomepageUrl = "http://javid.ddns.net/tModLoader/moddescription.php";
		internal const string PopularUrl = "http://javid.ddns.net/tModLoader/tools/populartop10.php";
		internal const string HotUrl = "http://javid.ddns.net/tModLoader/BotUtils/hottop10.php";

		private static string ModDir =>
			Path.Combine(AppContext.BaseDirectory, "mods");

		public static string ModPath(string modname) =>
			Path.Combine(ModDir, $"{modname}.json");

		public static IEnumerable<string> Mods =>
			Directory.GetFiles(ModDir, "*.json")
				.Select(x => Path.GetFileNameWithoutExtension(x).RemoveWhitespace());

		public static async Task Initialize()
		{
			//Mods = new List<Mod>();
		}

		//public static bool HasMod(Mod mod)
		// => Mods.Contains(mod);

		//public static Mod Get(int i)
		// => Mods.Count <= i ? Mods[i] : throw new ArgumentOutOfRangeException(nameof(i));

		/// <summary>
		/// Maintains mod data
		/// </summary>
		public static async Task Maintain(IDiscordClient client)
		{
			// Create dirs
			Directory.CreateDirectory(ModDir);

			var path = Path.Combine(ModDir, "date.txt");
			var dateDiff = TimeSpan.MinValue;

			// Data.txt present, read
			if (File.Exists(path))
			{
				var savedBinary = await BotUtils.FileReadToEndAsync(Semaphore, path);
				var parsedBinary = long.Parse(savedBinary);
				var savedBinaryDate = BotUtils.DateTimeFromUnixTimestampSeconds(parsedBinary);
				dateDiff = BotUtils.DateTimeFromUnixTimestampSeconds(BotUtils.GetCurrentUnixTimestampSeconds()) - savedBinaryDate;
			}

			// Needs to maintain data
			if (dateDiff == TimeSpan.MinValue
				|| dateDiff.TotalHours > 8d)
			{
				var data = await DownloadData();
				var modlist = JObject.Parse(data).SelectToken("modlist").ToObject<JArray>();

				foreach (var jtoken in modlist)
				{
					var name = jtoken.SelectToken("name").ToObject<string>().RemoveWhitespace();
					var jsonPath = Path.Combine(ModDir, $"{name}.json");
					await BotUtils.FileWriteAsync(Semaphore, jsonPath, jtoken.ToString(Formatting.Indented));
				}

				await BotUtils.FileWriteAsync(Semaphore, path, BotUtils.GetCurrentUnixTimestampSeconds().ToString());
			}
		}

		public static async Task<bool> TryCacheMod(string name)
		{
			var data = await DownloadSingleData(name);
			if (data.StartsWith("Failed:"))
				return false;

			var mod = JObject.Parse(data);
			await BotUtils.FileWriteAsync(Semaphore, Path.Combine(ModDir, $"{mod.SelectToken("name").ToObject<string>()}.json"), mod.ToString(Formatting.Indented));
			return true;
		}

		/// <summary>
		/// Will download mod json data
		/// </summary>
		private static async Task<string> DownloadData()
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				//var version = await GetTMLVersion();

				var values = new Dictionary<string, string>
				{
					{ "modloaderversion", $"tModLoader {GetTMLVersion()}" },
					{ "platform", "w"}
				};
				var content = new System.Net.Http.FormUrlEncodedContent(values);
				var response = await client.PostAsync(XmlUrl, content);
				var postResponse = await response.Content.ReadAsStringAsync();
				return postResponse;
			}
		}

		private static async Task<string> DownloadSingleData(string name)
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var response = await client.GetAsync(ModInfoUrl + $"?modname={name}");
				var postResponse = await response.Content.ReadAsStringAsync();
				return postResponse;
			}
		}

		//Very nasty, needs something better
		public static async Task<string> GetTMLVersion()
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var response =
					await client.GetAsync(
						"https://raw.githubusercontent.com/bluemagic123/tModLoader/master/solutions/CompleteRelease.bat");
				var postResponse = await response.Content.ReadAsStringAsync();
				return postResponse.Split('\n')[7].Split('=')[1];
			}
		}
	}
}
