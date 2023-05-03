using System.Net.Http;
using System.Threading.Tasks;

namespace Kalkatos.Network
{
	public class HttpClientCommunicator : ICommunicator
	{
		private HttpClient httpClient = new HttpClient();

		public async Task<string> Get (string url)
		{
			var response = await httpClient.GetAsync(url);
			return await response.Content.ReadAsStringAsync();
		}

		public async Task<string> Post (string url, string message)
		{
			var response = await httpClient.PostAsync(url, new StringContent(message));
			return await response.Content.ReadAsStringAsync();
		}
	}
}