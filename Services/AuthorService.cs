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
		
		internal const string AuthorInfoUrl = "https://tmlapis.tomat.dev/1.4/author/";
		internal const string LegacyAuthorInfoUrl = "https://tmlapis.tomat.dev/1.3/author/";

		
		public async Task<string> DownloadSingleData(string name)
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var response = await client.GetAsync(AuthorInfoUrl + name);
				string postResponse = await response.Content.ReadAsStringAsync();
				return postResponse;
			}
		}
		
		public async Task<string> DownloadSingleLegacyData(string name)
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var response = await client.GetAsync(LegacyAuthorInfoUrl + name);
				string postResponse = await response.Content.ReadAsStringAsync();
				return postResponse;
			}
		}
	}
}