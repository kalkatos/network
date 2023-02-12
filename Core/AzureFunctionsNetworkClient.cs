using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kalkatos.Network.Model;
using Newtonsoft.Json;

namespace Kalkatos.Network
{

	public class AzureFunctionsNetworkClient : NetworkEventDispatcher, INetworkClient
	{
		private HttpClient httpClient = new HttpClient();
		private DateTime lastCheckMatchTime;
		private int delayBetweenChecks = 2;

		public string MyId { get; private set; }
		public bool IsConnected { get; private set; }
		public bool IsInRoom { get; private set; }
		public PlayerInfo[] Players { get; private set; }
		public PlayerInfo MyInfo { get; private set; }
		public MatchInfo MatchInfo { get; private set; }
		public StateInfo StateInfo { get; private set; }

		// =========================================================  P U B L I C  ==============================================================

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

			var changedData = (Dictionary<string, string>)parameter;
			SetPlayerDataRequest request = new SetPlayerDataRequest
			{
				PlayerId = MyId,
				Data = changedData
			};
			_ = httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/SetPlayerData",
					"http://localhost:7089/api/SetPlayerData",
					new StringContent(JsonConvert.SerializeObject(request)));
			FireEvent((byte)NetworkEventKey.SetPlayerData, changedData);
		}

		public void FindMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			_ = FindMatchAsync(onSuccess, onFailure);
		}

		public void GetMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			_ = GetMatchAsync(onSuccess, onFailure);
		}

		public void LeaveMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			_ = LeaveMatchAsync(onSuccess, onFailure);
		}

		public void SendAction (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (parameter == null)
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is null, it must be an identifier string to connect." });
				return;
			}

			if (!(parameter is ActionRequest))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is not of the expected type." });
				return;
			}

			_ = SendActionAsync(JsonConvert.SerializeObject(parameter), onSuccess, onFailure);
		}

		public void GetMatchState (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (parameter == null)
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is null, it must be an identifier string to connect." });
				return;
			}

			if (!(parameter is StateRequest))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is not of the expected type." });
				return;
			}

			_ = GetMatchStateAsync(JsonConvert.SerializeObject(parameter), onSuccess, onFailure);
		}

		public void Get (byte key, object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			
		}

		public void Post (byte key, object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			
		}

		// ========================================================  P R I V A T E ===============================================================

		private async Task ConnectAsync (string connectInfoSerialized, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var response = await httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/LogIn",
					"http://localhost:7089/api/LogIn",
					new StringContent(connectInfoSerialized));
				string result = await response.Content.ReadAsStringAsync();
				LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(result);
				if (loginResponse == null || loginResponse.IsError)
				{
					string message = loginResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(Connect)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message });
				}
				else
				{
					IsConnected = true;
					MyId = loginResponse.PlayerId;
					MyInfo = new PlayerInfo
					{
						Alias = loginResponse.PlayerAlias,
						Nickname = loginResponse.SavedNickname,
					};
					onSuccess?.Invoke(loginResponse);
					FireEvent((byte)NetworkEventKey.Connect, loginResponse);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(Connect)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected to the internet." });
			}
		}

		private async Task FindMatchAsync (Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var response = await httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/FindMatch",
					"http://localhost:7089/api/FindMatch",
					new StringContent(MyId));
				string result = await response.Content.ReadAsStringAsync();
				Response findMatchResponse = JsonConvert.DeserializeObject<Response>(result);
				if (findMatchResponse == null || findMatchResponse.IsError)
				{
					string message = findMatchResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(FindMatch)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message }); 
				}
				else
				{
					onSuccess?.Invoke(null);
					lastCheckMatchTime = DateTime.UtcNow;
					FireEvent((byte)NetworkEventKey.FindMatch, null);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(FindMatch)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected to the internet." });
			}
		}

		private async Task GetMatchAsync (Action<object> onSuccess, Action<object> onFailure)
		{
			// Wait if the last GetMatch were made not long ago
			await DelayBetweenMatchChecks();

			try
			{
				var response = await httpClient.PostAsync(
				//"https://kalkatos-games.azurewebsites.net/api/GetMatch",
				"http://localhost:7089/api/GetMatch",
				new StringContent(JsonConvert.SerializeObject(new MatchRequest { PlayerId = MyId, MatchId = MatchInfo?.MatchId ?? "" })));
				string result = await response.Content.ReadAsStringAsync();
				MatchResponse matchResponse = JsonConvert.DeserializeObject<MatchResponse>(result);
				if (matchResponse == null || matchResponse.IsError)
				{
					string message = matchResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(GetMatch)}: {message}");
					MatchInfo = null;
					onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotFound, Message = message }); 
				}
				else
				{
					Logger.Log($"Got match = {JsonConvert.SerializeObject(matchResponse)}");
					MatchInfo = new MatchInfo { MatchId = matchResponse.MatchId, Players = matchResponse.Players };
					onSuccess?.Invoke(MatchInfo);
					FireEvent((byte)NetworkEventKey.GetMatch, MatchInfo);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(GetMatch)}: {e}");
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
				if (matchResponse == null || matchResponse.IsError)
				{
					string message = matchResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(LeaveMatch)}: {message}");
					onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = message });
				}
				else
				{
					string matchId = matchResponse.MatchId != null ? matchResponse.MatchId : "<unidentified>";
					Logger.Log($"Left match {matchId}, Message = {matchResponse.Message}");
					onSuccess?.Invoke(matchResponse);
					FireEvent((byte)NetworkEventKey.LeaveMatch, matchResponse);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(LeaveMatch)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = $"Exception trying to leave match = {e.Message}" });
			}
		}

		private async Task SendActionAsync (string actionRequestSerialized, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var response = await httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/SendAction",
					"http://localhost:7089/api/SendAction",
					new StringContent(actionRequestSerialized));
				string result = await response.Content.ReadAsStringAsync();
				ActionResponse actionResponse = JsonConvert.DeserializeObject<ActionResponse>(result);
				if (actionResponse == null || actionResponse.IsError)
				{
					string message = actionResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(SendAction)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message }); 
				}
				else
				{
					StateInfo = actionResponse.AlteredState;
					onSuccess?.Invoke(StateInfo);
					FireEvent((byte)NetworkEventKey.SendAction, StateInfo);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(SendAction)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error sending action." });
			}
		}

		private async Task GetMatchStateAsync (string stateRequestSerialized, Action<object> onSuccess, Action<object> onFailure)
		{
			await DelayBetweenMatchChecks();

			try
			{
				var response = await httpClient.PostAsync(
					//"https://kalkatos-games.azurewebsites.net/api/GetMatchState",
					"http://localhost:7089/api/GetMatchState",
					new StringContent(stateRequestSerialized));
				string result = await response.Content.ReadAsStringAsync();
				StateResponse stateResponse = JsonConvert.DeserializeObject<StateResponse>(result);
				if (stateResponse == null || stateResponse.IsError)
				{
					string message = stateResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(GetMatchState)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message }); 
				}
				else
				{
					StateInfo = stateResponse.StateInfo;
					onSuccess?.Invoke(StateInfo);
					FireEvent((byte)NetworkEventKey.GetMatchState, StateInfo);
					Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Got match state === {JsonConvert.SerializeObject(StateInfo)}");
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(GetMatchState)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error getting match state." });
			}
		}

		private async Task DelayBetweenMatchChecks ()
		{
			double timeSinceLastCheckMatch = (DateTime.UtcNow - lastCheckMatchTime).TotalSeconds;
			if (timeSinceLastCheckMatch < delayBetweenChecks)
				await Task.Delay((int)(delayBetweenChecks - timeSinceLastCheckMatch) * 1000);
			lastCheckMatchTime = DateTime.UtcNow;
		}

		// =======================================================  S U B C L A S S E S  ========================================================

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