// (c) 2023 Alex Kalkatos
// This code is licensed under MIT license (see LICENSE.txt for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kalkatos.Network.Model;
using Newtonsoft.Json;

namespace Kalkatos.Network
{
	/// <summary>
	/// Network Client implementation using Azure Functions.
	/// </summary>
	public class HttpFunctionsNetworkClient : INetworkClient
	{
		private Dictionary<string, string> urls = new Dictionary<string, string>
		{
			{ "SetPlayerData", "SetPlayerData" },
			{ "LogIn", "LogIn" },
			{ "FindMatch", "FindMatch" },
			{ "GetMatch", "GetMatch" },
			{ "LeaveMatch", "LeaveMatch" },
			{ "SendAction", "SendAction" },
			{ "GetMatchState", "GetMatchState" },
			{ "GetGameSettings", "GetGameSettings" },
			{ "GetData", "GetData" },
			{ "SetData", "SetData" },
			{ "GetBatchData", "GetBatchData" },
		};
		private ICommunicator communicator;

		public HttpFunctionsNetworkClient (ICommunicator communicator)
		{
			this.communicator = communicator;
			GameSettings = new();
		}

		public HttpFunctionsNetworkClient (ICommunicator communicator, Dictionary<string, string> urlDict) : this(communicator)
		{
			SetUrls(urlDict);
		}

		public string MyId { get; private set; }
		public bool IsConnected { get; private set; }
		public bool IsInRoom { get; private set; }
		public PlayerInfo[] Players { get; private set; }
		public PlayerInfo MyInfo { get; private set; }
		public MatchInfo MatchInfo { get; private set; }
		public StateInfo StateInfo { get; private set; }
		public Dictionary<string, string> GameSettings { get; private set; }

		// ████████████████████████████████████████████ P U B L I C ████████████████████████████████████████████

		public void SetUrls (Dictionary<string, string> urlDict)
		{
			foreach (var kv in urlDict)
			{
				string urlKey = kv.Key;
				if (urls.ContainsKey(urlKey))
					urls[urlKey] = kv.Value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param screenName="parameter">A <typeparamref screenName="LoginRequest"/></param>
		/// <param screenName="onSuccess">A <typeparamref screenName="LoginResponse"/> with matchResponse on the connection.</param>
		/// <param screenName="onFailure">A <typeparamref screenName="NetworkError"/> with the reason it did not connect.</param>
		public void Connect (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!CheckParameter<LoginRequest>(parameter, out string message, true))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = message });
				return;
			}

			Injection.Bind<INetworkClient>(this);

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
			if (!CheckParameter<Dictionary<string, string>>(parameter, out string message))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = message });
				return;
			}
			
			var changedData = (Dictionary<string, string>)parameter;
			SetPlayerDataRequest request = new SetPlayerDataRequest
			{
				PlayerId = MyId,
				Data = changedData
			};

			_ = SetPlayerDataAsync(request, onSuccess, onFailure);
		}

		public void FindMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!CheckParameter<FindMatchRequest>(parameter, out string message))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = message });
				return;
			}

			_ = FindMatchAsync((FindMatchRequest)parameter, onSuccess, onFailure);
		}

		public void GetMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!CheckParameter<MatchRequest>(parameter, out string message))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = message });
				return;
			}

			_ = GetMatchAsync((MatchRequest)parameter, onSuccess, onFailure);
		}

		public void LeaveMatch (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!CheckParameter<MatchRequest>(parameter, out string message))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = message });
				return;
			}

			_ = LeaveMatchAsync((MatchRequest)parameter, onSuccess, onFailure);
		}

		public void SendAction (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!CheckParameter<ActionRequest>(parameter, out string message))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = message });
				return;
			}

			_ = SendActionAsync(JsonConvert.SerializeObject(parameter), onSuccess, onFailure);
		}

		public void GetMatchState (object parameter, Action<object> onSuccess, Action<object> onFailure)
		{
			if (!CheckParameter<StateRequest>(parameter, out string message))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = message });
				return;
			}

			_ = GetMatchStateAsync(JsonConvert.SerializeObject(parameter), onSuccess, onFailure);
		}

		public void GetData (string key, string defaultValue, Action<object> onSuccess, Action<object> onFailure)
		{
			_ = GetDataAsync(key, defaultValue, onSuccess, onFailure);
		}

		public void SetData (string key, string value, Action<object> onSuccess, Action<object> onFailure)
		{
			_ = SetDataAsync(key, value, onSuccess, onFailure);
		}

		public void GetBatchData (string query, Action<object> onSuccess, Action<object> onFailure)
		{
			_ = GetBatchDataAsync(query, onSuccess, onFailure);
		}

		/// ████████████████████████████████████████████ P R I V A T E ████████████████████████████████████████████

		private bool CheckParameter<T>(object parameter, out string message, bool skipConnected = false)
		{
			if (!IsConnected && !skipConnected)
			{
				message = "Not connected.";
				Logger.LogError(message);
				return false;
			}
			if (parameter == null)
			{
				message = $"Parameter is null. It must be of type {typeof(T)}.";
				return false;
			}
			if (!(parameter is T))
			{
				message = $"Parameter is not of the expected type ({typeof(T)}).";
				return false;
			}
			message = "OK";
			return true;
		}

		private async Task<string> Post (string uriTag, string content)
		{
			TaskCompletionSource<string> taskAwaiter = new TaskCompletionSource<string>();
			communicator.Post(urls[uriTag], content, response => taskAwaiter.TrySetResult(response));
			return await taskAwaiter.Task;
		}

		private void ClearMatchData ()
		{
			MatchInfo = null;
			IsInRoom = false;
			Players = null;
			StateInfo = null;
		}

		private async Task ConnectAsync (LoginRequest request, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				string serializedRequest = JsonConvert.SerializeObject(request);
				string result = await Post("LogIn", serializedRequest);
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Result received: {result}");
				LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(result);
				if (loginResponse == null || loginResponse.IsError)
				{
					string message = loginResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(Connect)}: {message}");
					if (message.Contains("Version"))
						onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.OutdatedVersion, Message = message });
					else
						onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotAvailable, Message = message });
				}
				else
				{
					string configResult = await Post("GetGameSettings", JsonConvert.SerializeObject(new GameDataRequest
					{
						GameId = request.GameId,
						PlayerId = loginResponse.PlayerId
					}));
					GameDataResponse gameDataResponse = JsonConvert.DeserializeObject<GameDataResponse>(configResult);
					if (gameDataResponse?.Settings != null)
					{
						GameSettings = gameDataResponse.Settings;
						foreach (var item in gameDataResponse.Settings)
							if (urls.ContainsKey(item.Key))
								urls[item.Key] = gameDataResponse.Settings[item.Key];
					}
					else
						Logger.Log("Error getting game settings from server.");

					IsConnected = true;
					MyId = loginResponse.PlayerId;
					MyInfo = loginResponse.MyInfo;
					onSuccess?.Invoke(loginResponse);
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Logged in with player: {JsonConvert.SerializeObject(MyInfo)}");
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(Connect)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotAvailable, Message = "Server internal error." });
			}
		}

		private async Task SetPlayerDataAsync (SetPlayerDataRequest request, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				string result = await Post("SetPlayerData", JsonConvert.SerializeObject(request));
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Result received: {result}");
				PlayerInfoResponse playerInfoResponse = JsonConvert.DeserializeObject<PlayerInfoResponse>(result);
				if (playerInfoResponse == null || playerInfoResponse.IsError)
				{
					string message = playerInfoResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(SetPlayerData)}: {message}");
					onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotAvailable, Message = message });
				}
				else
				{
					MyInfo = playerInfoResponse.PlayerInfo;
					onSuccess?.Invoke(MyInfo);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(SetPlayerDataAsync)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected to the internet." });
			}
		}

		private async Task FindMatchAsync (FindMatchRequest request, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				string result = await Post("FindMatch", JsonConvert.SerializeObject(request));
				Response findMatchResponse = JsonConvert.DeserializeObject<Response>(result);
				if (findMatchResponse == null || findMatchResponse.IsError)
				{
					string message = findMatchResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(FindMatch)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message }); 
				}
				else
				{
					ClearMatchData();
					onSuccess?.Invoke(null);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(FindMatch)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected to the internet." });
			}
		}

		private async Task GetMatchAsync (MatchRequest request, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				string result = await Post("GetMatch", JsonConvert.SerializeObject(request));
				MatchResponse matchResponse = JsonConvert.DeserializeObject<MatchResponse>(result);
				if (matchResponse == null || matchResponse.IsError)
				{
					string message = matchResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(GetMatch)}: {message}");
					ClearMatchData();
					if (message.Contains("already started"))
						onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.AlreadyStarted, Message = message });
					else if (message.Contains("is full"))
						onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.IsFull, Message = message });
					else
						onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotFound, Message = message }); 
				}
				else
				{
					Logger.Log($"Got match = {JsonConvert.SerializeObject(matchResponse)}");
					IsInRoom = true;
					MatchInfo = matchResponse.MatchInfo;
					Players = MatchInfo.Players;
					onSuccess?.Invoke(MatchInfo);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(GetMatch)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error getting match." });
			}
		}

		private async Task LeaveMatchAsync (MatchRequest request, Action<object> onSuccess, Action<object> onFailure)
		{
			ClearMatchData();
			try
			{
				string result = await Post("LeaveMatch", JsonConvert.SerializeObject(request));
				Response leaveMatchResponse = JsonConvert.DeserializeObject<Response>(result);
				if (leaveMatchResponse == null || leaveMatchResponse.IsError)
				{
					string message = leaveMatchResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(LeaveMatch)}: {message}");
					onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = message });
				}
				else
				{
					string matchId = MatchInfo?.MatchId ?? "<unknown>";
					Logger.Log($"Left match {matchId}, Message = {leaveMatchResponse.Message}");
					onSuccess?.Invoke(leaveMatchResponse);
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(LeaveMatch)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = $"Exception trying to leave match = {e.Message}" });
			}
		}

		private async Task SendActionAsync (string actionRequestSerialized, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				string result = await Post("SendAction", actionRequestSerialized);
				ActionResponse actionResponse = JsonConvert.DeserializeObject<ActionResponse>(result);
				if (actionResponse == null || actionResponse.IsError)
				{
					string message = actionResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(SendAction)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message }); 
				}
				else
				{
					onSuccess?.Invoke("Success");
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(SendAction)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error sending action." });
			}
		}

		private async Task GetMatchStateAsync (string stateRequestSerialized, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				string result = await Post("GetMatchState", stateRequestSerialized);
				StateResponse stateResponse = JsonConvert.DeserializeObject<StateResponse>(result);
				if (stateResponse == null || stateResponse.IsError)
				{
					string message = stateResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(GetMatchState)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message });
				}
				else
				{
					StateInfo = stateResponse.StateInfo;
					onSuccess?.Invoke(StateInfo);
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Got match state === {JsonConvert.SerializeObject(StateInfo)}");
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(GetMatchState)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error getting match state." });
			}
		}

		private async Task GetDataAsync (string key, string defaultValue, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				DataRequest request = new DataRequest
				{
					PlayerId = MyId,
					Key = key,
					DefaultValue = defaultValue
				};
				string result = await Post("GetData", JsonConvert.SerializeObject(request));
				DataResponse dataResponse = JsonConvert.DeserializeObject<DataResponse>(result);
				if (dataResponse == null || dataResponse.IsError)
				{
					string message = dataResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(GetData)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message });
				}
				else
				{
					onSuccess?.Invoke(dataResponse.Data);
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Got data === {JsonConvert.SerializeObject(dataResponse.Data)}");
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(GetData)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error getting data." });
			}
		}

		private async Task SetDataAsync (string key, string value, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				DataRequest request = new DataRequest
				{
					PlayerId = MyId,
					Key = key,
					Value = value,
				};
				string result = await Post("SetData", JsonConvert.SerializeObject(request));
				DataResponse dataResponse = JsonConvert.DeserializeObject<DataResponse>(result);
				if (dataResponse == null || dataResponse.IsError)
				{
					string message = dataResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(SetData)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message });
				}
				else
				{
					onSuccess?.Invoke("Data set successfully");
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Data set === {result}");
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(SetData)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error setting data." });
			}
		}

		private async Task GetBatchDataAsync (string query, Action<object> onSuccess, Action<object> onFailure)
		{
			try
			{
				DataBatchRequest request = new DataBatchRequest
				{
					PlayerId = MyId,
					Query = query,
				};
				string result = await Post("GetBatchData", JsonConvert.SerializeObject(request));
				DataBatchResponse dataResponse = JsonConvert.DeserializeObject<DataBatchResponse>(result);
				if (dataResponse == null || dataResponse.IsError)
				{
					string message = dataResponse?.Message ?? "Server internal error";
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(GetBatchData)}: {message}");
					onFailure?.Invoke(new NetworkError { Message = message });
				}
				else
				{
					onSuccess?.Invoke(dataResponse.Data);
					Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Data batch got === {result}");
				}
			}
			catch (Exception e)
			{
				Logger.Log($"[{nameof(HttpFunctionsNetworkClient)}] Error in {nameof(GetBatchData)}: {e}");
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.Undefined, Message = "Error getting batch data." });
			}
		}
	}
}