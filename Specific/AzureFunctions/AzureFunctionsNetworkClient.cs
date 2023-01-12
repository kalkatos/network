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

		private bool isInitialized = false;
		private HttpClient httpClient = new HttpClient();
		private string currentRoomAlias;
		private DateTime lastCheckMatchTime;
		private int delayBetweenChecks = 5;

		public bool IsConnected { get; private set; }
		public bool IsInRoom { get; private set; }
		public PlayerInfo[] Players { get; private set; }
		public PlayerInfo MyInfo { get; private set; }
		public RoomInfo RoomInfo { get; private set; }

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
			if (!CheckParameter(parameter, Type.GetTypeCode(typeof(PlayerConnectInfo)), ref error))
			{
				onFailure?.Invoke(error);
				return;
			}

			_ = ConnectAsync(JsonConvert.SerializeObject(parameter), onSuccess, onFailure);
		}

		public void FindMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			NetworkError error = new NetworkError();
			if (!CheckParameter(parameter, TypeCode.String, ref error))
			{
				onFailure?.Invoke(error);
				return;
			}

			_ = FindMatchAsync((string)parameter, onSuccess, onFailure);
		}

		public void LeaveMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			NetworkError error = new NetworkError();
			if (!CheckParameter(parameter, TypeCode.String, ref error))
			{
				onFailure?.Invoke(error);
				return;
			}
		}

		public void GetMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			NetworkError error = new NetworkError();
			if (!CheckParameter(parameter, TypeCode.String, ref error))
			{
				onFailure?.Invoke(error);
				return;
			}

			_ = GetMatchAsync((string)parameter, onSuccess, onFailure);
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
			if (isInitialized)
				return;
			isInitialized = true;
			httpClient.Timeout = TimeSpan.FromSeconds(5);
		}

		public bool CheckParameter (object parameter, TypeCode type, ref NetworkError networkError)
		{
			if (parameter == null)
			{
				networkError = new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is null, it must be an identifier string to connect." };
				return false;
			}

			if (Type.GetTypeCode(parameter.GetType()) != type)
			{
				networkError = new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is not of the expected type." };
				return false;
			}

			return true;
		}

		private async Task ConnectAsync (string connectInfoSerialized, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var response = await httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/LogIn",
					"http://localhost:7089/api/LogIn",
					new StringContent(connectInfoSerialized));
				string result = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode)
				{
					LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(result);
					IsConnected = true;
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
				Logger.LogError(e.ToString());
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected to the internet." });
			}
		}

		private async Task FindMatchAsync (string playerId, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var response = await httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/FindMatch",
					"http://localhost:7089/api/FindMatch",
					new StringContent(playerId));
				string result = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode)
				{
					onSuccess?.Invoke(null);
					lastCheckMatchTime = DateTime.UtcNow;
				}
				else
				{
					NetworkError? error = JsonConvert.DeserializeObject<NetworkError>(result);
					onFailure?.Invoke(error);
				}
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString());
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected to the internet." });
			}

			await Task.Delay(delayBetweenChecks * 1000);
			_ = GetMatchAsync(playerId, null, null);
		}

		private async Task GetMatchAsync (string playerId, Action<object> onSuccess, Action<object> onFailure)
		{
			// Wait if the last GetMatch were made not long ago
			// TODO Wait full time only if it's the first get after FindMatch, otherwise, wait just 1 or 2 seconds
			double timeSinceLastCheckMatch = (DateTime.UtcNow - lastCheckMatchTime).TotalSeconds;
			if (timeSinceLastCheckMatch < delayBetweenChecks)
				await Task.Delay((int)(delayBetweenChecks - timeSinceLastCheckMatch) * 1000);
			lastCheckMatchTime = DateTime.UtcNow;

			try
			{
				var response = await httpClient.PostAsync(
				//"https://kalkatos-games.azurewebsites.net/api/GetMatch",
				"http://localhost:7089/api/GetMatch",
				new StringContent(playerId));
				string result = await response.Content.ReadAsStringAsync();
				if (!string.IsNullOrEmpty(result))
				{
					RoomInfo? info = JsonConvert.DeserializeObject<RoomInfo?>(result);
					if (info.HasValue)
						RoomInfo = info.Value;
				}
				if (string.IsNullOrEmpty(RoomInfo.RoomId))
					onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotFound, Message = "No match found for this client." });
				else
					onSuccess?.Invoke(RoomInfo);
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString());
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error getting match." });
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