using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kalkatos.Network
{
	public class HttpClientCommunicator : ICommunicator
	{
		private HttpClient httpClient = new HttpClient();

		public void Get (string url, Action<string> callback)
		{
			_ = GetAsync(url, callback);
		}

		public void Post (string url, string message, Action<string> callback)
		{
			_ = PostAsync(url, message, callback);
		}

		private async Task GetAsync (string url, Action<string> callback)
		{
			var response = await httpClient.GetAsync(url);
			string result = await response.Content.ReadAsStringAsync();
			callback?.Invoke(result);
		}

		private async Task PostAsync (string url, string message, Action<string> callback)
		{
			var response = await httpClient.PostAsync(url, new StringContent(message));
			string result = await response.Content.ReadAsStringAsync();
			callback?.Invoke(result);
		}
	}
}