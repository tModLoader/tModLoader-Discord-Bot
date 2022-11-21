using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace tModloaderDiscordBot.Services
{
	public class AuthorService : BaseService
	{
		public AuthorService(IServiceProvider services) : base(services)
		{
		}
		
		private const string AuthorInfoUrl = "https://tmlapis.tomat.dev/1.4/author/";
		private const string LegacyAuthorInfoUrl = "https://tmlapis.tomat.dev/1.3/author/";
		internal const string WidgetUrl = "https://tml-readme-card.repl.co/?steamid64=";
		internal const string LegacyWidgetUrl = "https://tml-readme-card.repl.co/?v=1.3&steamid64=";

		
		private static async Task<string> GetString(string url)
		{
			using var client = new System.Net.Http.HttpClient();
			
			var response = await client.GetAsync(url);
			string postResponse = await response.Content.ReadAsStringAsync();
			return postResponse;
		}

		public async Task<string> DownloadSingleData(string name)
		{
			return await GetString(AuthorInfoUrl + name);
		}
		
		public async Task<string> DownloadSingleLegacyData(string name)
		{
			return await GetString(LegacyAuthorInfoUrl + name);
		}
	}
}