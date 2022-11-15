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
		
		internal const string ModInfoUrl = "https://tmlapis.tomat.dev/1.4/author/";
		
		public async Task<string> DownloadSingleData(string name)
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var response = await client.GetAsync(ModInfoUrl + name);
				string postResponse = await response.Content.ReadAsStringAsync();
				return postResponse;
			}
		}
	}
}