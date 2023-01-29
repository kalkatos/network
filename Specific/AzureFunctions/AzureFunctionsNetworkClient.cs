using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Kalkatos.Network.Model;
using Newtonsoft.Json;

namespace Kalkatos.Network.Specific
{
	public class AzureFunctionsNetworkClient : INetworkClient
	{
		public event Action<byte, object> OnEventReceived;

		private HttpClient httpClient = new HttpClient();
		private DateTime lastCheckMatchTime;
		private int delayForFirstCheck = 8;
		private int delayBetweenChecks = 3;

		public string MyId { get; private set; }
		public bool IsConnected { get; private set; }
		public bool IsInRoom { get; private set; }
		public PlayerInfo[] Players { get; private set; }
		public PlayerInfo MyInfo { get; private set; }
		public MatchInfo MatchInfo { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param screenName="parameter">A <typeparamref screenName="LoginRequest"/></param>
		/// <param screenName="onSuccess">A <typeparamref screenName="LoginResponse"/> with matchResponse on the connection.</param>
		/// <param screenName="onFailure">A <typeparamref screenName="NetworkError"/> with the reason it did not connect.</param>
		public void Connect (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (parameter == null)
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is null, it must be an identifier string to connect." });
				return;
			}

			if (!(parameter is LoginRequest))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is not of the expected type." });
				return;
			}

			_ = ConnectAsync(JsonConvert.SerializeObject(parameter), onSuccess, onFailure);
		}

		public void SetNickname (string nickname)
		{
			if (!IsConnected)
			{
				Logger.LogError("Not connected.");
				return;
			}
			SetPlayerData(new Dictionary<string, string> { { "Nickname", nickname } }, null, null);
		}

		public void SetPlayerData (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!IsConnected)
			{
				Logger.LogError("Not connected.");
				return;
			}

			if (!(parameter is Dictionary<string, string>))
			{
				Logger.LogError("Wrong parameter. Must be a Dictionary<string, string>.");
				return;
			}

			SetPlayerDataRequest request = new SetPlayerDataRequest
			{
				PlayerId = MyId,
				Data = (Dictionary<string, string>)parameter
			};
			_ = httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/SetPlayerData",
					"http://localhost:7089/api/SetPlayerData",
					new StringContent(JsonConvert.SerializeObject(request)));
		}

		public void FindMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			_ = FindMatchAsync(onSuccess, onFailure);
		}

		public void LeaveMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			_ = LeaveMatchAsync(onSuccess, onFailure);
		}

		public void GetMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			_ = GetMatchAsync(onSuccess, onFailure);
		}

		public void Get (byte key, object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			
		}

		public void Post (byte key, object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			
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
					MyId = loginResponse.PlayerId;
					MyInfo = new PlayerInfo
					{
						Alias = loginResponse.PlayerAlias,
						Nickname = loginResponse.SavedNickname,
					};
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

		private async Task FindMatchAsync (Action<object> onSuccess, Action<object> onFailure)
		{
			Logger.Log("Trying to find a match.");
			try
			{
				var response = await httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/FindMatch",
					"http://localhost:7089/api/FindMatch",
					new StringContent(MyId));
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

			await Task.Delay(delayForFirstCheck * 1000);
			_ = GetMatchAsync(null, null);
		}

		private async Task GetMatchAsync (Action<object> onSuccess, Action<object> onFailure)
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
				new StringContent(JsonConvert.SerializeObject(new MatchRequest { PlayerId = MyId, MatchId = MatchInfo?.MatchId ?? "" })));
				string result = await response.Content.ReadAsStringAsync();
				MatchResponse matchResponse = JsonConvert.DeserializeObject<MatchResponse>(result);
				if (matchResponse.IsError)
				{
					Logger.Log($"Couldn't get match. Error = {matchResponse.Message}");
					MatchInfo = null;
					onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotFound, Message = matchResponse.Message }); 
				}
				else
				{
					Logger.Log($"Got match = {JsonConvert.SerializeObject(matchResponse)}");
					MatchInfo = new MatchInfo { MatchId = matchResponse.MatchId, Players = matchResponse.Players };
					onSuccess?.Invoke(MatchInfo); 
				}
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString());
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error getting match." });
			}
		}

		private async Task LeaveMatchAsync (Action<object> onSuccess, Action<object> onFailure)
		{
			MatchInfo = null;
			try
			{
				var response = await httpClient.PostAsync(
				//"https://kalkatos-games.azurewebsites.net/api/LeaveMatch",
				"http://localhost:7089/api/LeaveMatch",
				new StringContent(JsonConvert.SerializeObject(new MatchRequest { PlayerId = MyId, MatchId = MatchInfo?.MatchId ?? "" })));
				string result = await response.Content.ReadAsStringAsync();
				MatchResponse matchResponse = JsonConvert.DeserializeObject<MatchResponse>(result);
				if (matchResponse.IsError)
				{
					Logger.Log($"Error trying to leave match = {matchResponse.Message}");
					onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = matchResponse.Message });
				}
				else
				{
					string matchId = matchResponse.MatchId != null ? matchResponse.MatchId : "<unidentified>";
					Logger.Log($"Left match {matchId}, Message = {matchResponse.Message}");
					onSuccess?.Invoke(null);
				}
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString());
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = $"Exception trying to leave match = {e.Message}" });
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