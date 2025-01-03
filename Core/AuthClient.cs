using Kalkatos.Network.Model;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kalkatos.Network
{
	public class AuthClient : IDisposable
    {
        private AuthUrls urls;
		private HttpClient client;
		private string currentState;
		private string playerId;
		private string deviceId;

		public AuthClient (AuthUrls urls, string playerId, string deviceId)
        {
            this.urls = urls;
			this.playerId = playerId;
			this.deviceId = deviceId;
            client = new HttpClient();
        }

        ~AuthClient () => Dispose();

		public void Dispose () => client.Dispose();

        public async Task RequestAuthUrl (Action<string> onSuccess, Action onFailure)
        {
			string url = urls.GetAuthUrl;
			var response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);
			string content = await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
			{
				Logger.LogError($"Error getting auth url: {response.StatusCode} - {response.ReasonPhrase}. Content: {content}");
				onFailure?.Invoke();
				return;
			}
			currentState = ExtractStateFromUrl(content);
			Logger.Log($"State is {currentState}");
            onSuccess?.Invoke(content);
		}

        public async Task RequestUserInfo (Action<UserInfo> onSuccess, Action<string> onFailure)
        {
			var request = new AuthDataRequest
			{
				PlayerId = playerId,
				DeviceId = deviceId,
				State = currentState
			};
			string url = urls.LoginAuthUrl;
			var httpResponse = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(request)));
			string responseStr = await httpResponse.Content.ReadAsStringAsync();
			AuthDataResponse response = JsonConvert.DeserializeObject<AuthDataResponse>(responseStr);
			if (response.Status == "Concluded")
			{
				UserInfo info = new UserInfo
				{
					Name = response.Name,
					Email = response.Email,
					Picture = response.Picture
				};
				onSuccess?.Invoke(info);
                return;
			}
			onFailure?.Invoke(response.Message);
		}

		private string ExtractStateFromUrl (string url)
		{
			string stateHandle = "state=";
			int index = url.IndexOf(stateHandle);
			if (index == -1)
				throw new Exception($"Couldn't extract state from url: {url}");
			int start = index + stateHandle.Length;
			int end = url.IndexOf('&', start);
			if (end == -1)
				end = url.Length;
			return url.Substring(start, end - start);
		}
	}

	[Serializable]
	public class AuthUrls
	{
		public string GetAuthUrl;
		public string LoginAuthUrl;
	}
}
