using System;
using System.Collections.Generic;
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
		private Dictionary<string, string> uris = new Dictionary<string, string>
		{
			{ "SetPlayerData", "https://kalkatos-games.azurewebsites.net/api/SetPlayerData?code=oFl2jbDCTLC7yniharMjY1qjJpBq7tqArNehC-SHMa0sAzFuFMdPgg==" },
			{ "LogIn", "https://kalkatos-games.azurewebsites.net/api/LogIn?code=oFl2jbDCTLC7yniharMjY1qjJpBq7tqArNehC-SHMa0sAzFuFMdPgg==" },
			{ "FindMatch", "https://kalkatos-games.azurewebsites.net/api/FindMatch?code=oFl2jbDCTLC7yniharMjY1qjJpBq7tqArNehC-SHMa0sAzFuFMdPgg==" },
			{ "GetMatch", "https://kalkatos-games.azurewebsites.net/api/GetMatch?code=oFl2jbDCTLC7yniharMjY1qjJpBq7tqArNehC-SHMa0sAzFuFMdPgg==" },
			{ "LeaveMatch", "https://kalkatos-games.azurewebsites.net/api/LeaveMatch?code=oFl2jbDCTLC7yniharMjY1qjJpBq7tqArNehC-SHMa0sAzFuFMdPgg==" },
			{ "SendAction", "https://kalkatos-games.azurewebsites.net/api/SendAction?code=oFl2jbDCTLC7yniharMjY1qjJpBq7tqArNehC-SHMa0sAzFuFMdPgg==" },
			{ "GetMatchState", "https://kalkatos-games.azurewebsites.net/api/GetMatchState?code=oFl2jbDCTLC7yniharMjY1qjJpBq7tqArNehC-SHMa0sAzFuFMdPgg==" }

			//{ "SetPlayerData", "http://localhost:7089/api/SetPlayerData" },
			//{ "LogIn", "http://localhost:7089/api/LogIn" },
			//{ "FindMatch", "http://localhost:7089/api/FindMatch" },
			//{ "GetMatch", "http://localhost:7089/api/GetMatch" },
			//{ "LeaveMatch", "http://localhost:7089/api/LeaveMatch" },
			//{ "SendAction", "http://localhost:7089/api/SendAction" },
			//{ "GetMatchState", "http://localhost:7089/api/GetMatchState" }
		};

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

			_ = ConnectAsync((LoginRequest)parameter, onSuccess, onFailure);
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
			_ = httpClient.PostAsync(uris["SetPlayerData"],
					new StringContent(JsonConvert.SerializeObject(request)));
			FireEvent((byte)NetworkEventKey.SetPlayerData, changedData);
		}

		public void FindMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!IsConnected)
			{
				Logger.LogError("Not connected.");
				return;
			}

			if (!(parameter is FindMatchRequest))
			{
				Logger.LogError("Wrong parameter. Must be a Dictionary<string, string>.");
				return;
			}

			_ = FindMatchAsync((FindMatchRequest)parameter, onSuccess, onFailure);
		}

		public void GetMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!IsConnected)
			{
				Logger.LogError("Not connected.");
				return;
			}

			if (!(parameter is MatchRequest))
			{
				Logger.LogError("Wrong parameter. Must be a Dictionary<string, string>.");
				return;
			}

			_ = GetMatchAsync((MatchRequest)parameter, onSuccess, onFailure);
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

		private async Task ConnectAsync (LoginRequest connectInfo, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var responseIp = await httpClient.GetAsync("https://api.ipify.org");
				string resultIp = await responseIp.Content.ReadAsStringAsync();
				Logger.Log(resultIp);
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(Connect)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected to the internet." });
				return;
			}

			try
			{
				var response = await httpClient.PostAsync(uris["LogIn"],
					new StringContent(JsonConvert.SerializeObject(connectInfo)));
				string result = await response.Content.ReadAsStringAsync();
				LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(result);
				if (loginResponse == null || loginResponse.IsError)
				{
					string message = loginResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(Connect)}: {message}");
					onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotAvailable, Message = message });
				}
				else
				{
					IsConnected = true;
					MyId = loginResponse.PlayerId;
					MyInfo = loginResponse.MyInfo;
					onSuccess?.Invoke(loginResponse);
					FireEvent((byte)NetworkEventKey.Connect, loginResponse);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(Connect)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotAvailable, Message = "Server internal error." });
			}
		}

		private async Task FindMatchAsync (FindMatchRequest request, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var response = await httpClient.PostAsync(uris["FindMatch"], new StringContent(JsonConvert.SerializeObject(request)));
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

		private async Task GetMatchAsync (MatchRequest request, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				var response = await httpClient.PostAsync(uris["GetMatch"], new StringContent(JsonConvert.SerializeObject(request)));
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
				var response = await httpClient.PostAsync(uris["LeaveMatch"],
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
				var response = await httpClient.PostAsync(uris["SendAction"],
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
			try
			{
				var response = await httpClient.PostAsync(uris["GetMatchState"],
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