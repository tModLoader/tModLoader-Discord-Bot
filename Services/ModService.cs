using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;

namespace tModloaderDiscordBot.Services
{
	public class ModService : BaseService
	{
		public ModService(IServiceProvider services) : base(services)
		{
		}
		
		private const string ModInfoUrl = "https://tmlapis.tomat.dev/1.4/mod/";
		
		public async Task<string> DownloadSingleData(string name)
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
	}
}