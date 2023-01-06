using System;
using System.Net.Http;
using System.Threading.Tasks;
using Kalkatos.FunctionsGame.Models;
using Kalkatos.Network.Model;
using Newtonsoft.Json;

namespace Kalkatos.Network.Specific
{
	public class AzureFunctionsNetworkClient : INetworkClient
	{
		public event Action<byte, object> OnEventReceived;

		public bool IsConnected { get; set; }
		public bool IsInRoom { get; set; }
		public PlayerInfo[] Players { get; set; }
		public PlayerInfo MyInfo { get; set; }

		private bool _isInitialized = false;
		private HttpClient _httpClient = new HttpClient();

		/// <summary>
		/// 
		/// </summary>
		/// <param screenName="parameter">A string with a device identifier.</param>
		/// <param screenName="onSuccess">A <typeparamref screenName="LoginResponse"/> with info on the connection.</param>
		/// <param screenName="onFailure">A <typeparamref screenName="NetworkError"/> with the reason it did not connect.</param>
		public void Connect (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			Initialize();

			NetworkError error = new NetworkError();
			if (!CheckNullParameter(parameter, ref error))
			{
				onFailure?.Invoke(error);
				return; 
			}
			
			_ = ConnectAsync(parameter, onSuccess, onFailure);
		}

		public void FindMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			NetworkError error = new NetworkError();
			if (!CheckNullParameter(parameter, ref error))
			{
				onFailure?.Invoke(error);
				return;
			}

			_ = FindMatchAsync((string)parameter, onSuccess, onFailure);
		}

		public void LeaveMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			NetworkError error = new NetworkError();
			if (!CheckNullParameter(parameter, ref error))
			{
				onFailure?.Invoke(error);
				return;
			}
		}

		public void Get (byte key, object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			Initialize();
		}

		public void Post (byte key, object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			Initialize();
		}

		private void Initialize ()
		{
			if (_isInitialized)
				return;
			_isInitialized = true;
			_httpClient.Timeout = TimeSpan.FromSeconds(5);
		}

		public bool CheckNullParameter (object parameter, ref NetworkError networkError)
		{
			if (parameter == null)
			{
				networkError = new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is null, it must be an identifier string to connect." };
				return false;
			}

			if (!(parameter is string))
			{
				networkError = new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is not a string with an identifier." };
				return false;
			}

			return true;
		}

		private async Task ConnectAsync (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var response = await _httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/LogIn?code=DL6gIZIRhvoYe7OBizrkqfImdvJxcxXvRy0j8BNCzCvtAzFuJ9Lpbg==",
					"http://localhost:7089/api/LogIn",
					new StringContent((string)parameter));
				string result = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode)
				{
					LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(result);
					onSuccess?.Invoke(loginResponse);
				}
				else
				{
					NetworkError? error = JsonConvert.DeserializeObject<NetworkError>(result);
					onFailure?.Invoke(error);
				}
			}
			catch (Exception e)
			{
				Logger.LogError(e.Message);
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected to the internet." });
			}
		}

		private async Task FindMatchAsync (string playerId, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var response = await _httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/FindMatch?code=",
					"http://localhost:7089/api/FindMatch",
					new StringContent(playerId));
				string result = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode)
				{
					onSuccess?.Invoke(null);
				}
				else
				{
					NetworkError? error = JsonConvert.DeserializeObject<NetworkError>(result);
					onFailure?.Invoke(error);
				}
			}
			catch (Exception e)
			{
				Logger.LogError(e.Message);
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected to the internet." });
			}
		}

		public class FunctionInfo
		{
			public string FunctionName;
			public string Url;
		}

		public class Config
		{
			public FunctionInfo[] Functions;
		}
	}
}