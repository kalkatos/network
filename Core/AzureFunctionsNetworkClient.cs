using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kalkatos.Network.Model;
using Newtonsoft.Json;

namespace Kalkatos.Network
{
	public class AzureFunctionsNetworkClient : NetworkEventDispatcher, INetworkClient
	{
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

		private ICommunicator communicator;

		public AzureFunctionsNetworkClient (ICommunicator communicator)
		{
			this.communicator = communicator;
		}

		public string MyId { get; private set; }
		public bool IsConnected { get; private set; }
		public bool IsInRoom { get; private set; }
		public PlayerInfo[] Players { get; private set; }
		public PlayerInfo MyInfo { get; private set; }
		public MatchInfo MatchInfo { get; private set; }
		public StateInfo StateInfo { get; private set; }

		// ████████████████████████████████████████████ P U B L I C ████████████████████████████████████████████

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
			_ = communicator.Post(uris["SetPlayerData"], JsonConvert.SerializeObject(request));
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
				Logger.LogError("Wrong parameter. Must be a MatchRequest.");
				return;
			}

			_ = GetMatchAsync((MatchRequest)parameter, onSuccess, onFailure);
		}

		public void LeaveMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!IsConnected)
			{
				Logger.LogError("Not connected.");
				return;
			}

			if (!(parameter is MatchRequest))
			{
				Logger.LogError("Wrong parameter. Must be a MatchRequest.");
				return;
			}

			_ = LeaveMatchAsync((MatchRequest)parameter, onSuccess, onFailure);
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

		/// ████████████████████████████████████████████ P R I V A T E ████████████████████████████████████████████

		private async Task ConnectAsync (LoginRequest connectInfo, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				string result = await communicator.Post(uris["LogIn"], JsonConvert.SerializeObject(connectInfo));
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
				string result = await communicator.Post(uris["FindMatch"], JsonConvert.SerializeObject(request));
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
				string result = await communicator.Post(uris["GetMatch"], JsonConvert.SerializeObject(request));
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
					if (matchResponse.IsOver)
					{
						MatchInfo = null;
						onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotFound, Message = "Match is over." });
						return;
					}
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

		private async Task LeaveMatchAsync (MatchRequest request, Action<object> onSuccess, Action<object> onFailure)
		{
			MatchInfo = null;
			try
			{
				string result = await communicator.Post(uris["LeaveMatch"], JsonConvert.SerializeObject(request));
				Response leaveMatchResponse = JsonConvert.DeserializeObject<Response>(result);
				if (leaveMatchResponse == null || leaveMatchResponse.IsError)
				{
					string message = leaveMatchResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(AzureFunctionsNetworkClient)}] Error in {nameof(LeaveMatch)}: {message}");
					onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = message });
				}
				else
				{
					string matchId = MatchInfo?.MatchId ?? "<unknown>";
					Logger.Log($"Left match {matchId}, Message = {leaveMatchResponse.Message}");
					onSuccess?.Invoke(leaveMatchResponse);
					FireEvent((byte)NetworkEventKey.LeaveMatch, leaveMatchResponse);
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
				string result = await communicator.Post(uris["SendAction"], actionRequestSerialized);
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
				string result = await communicator.Post(uris["GetMatchState"], stateRequestSerialized);
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

		// ████████████████████████████████████████████ S U B C L A S S E S ████████████████████████████████████████████

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