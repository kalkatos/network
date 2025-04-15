// (c) 2023 Alex Kalkatos
// This code is licensed under MIT license (see LICENSE.txt for details)

#if UNITY_2018_1_OR_NEWER

using System;
using System.Collections.Generic;
using Kalkatos.Network.Model;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kalkatos.Network.Unity
{
	/// <summary>
	/// Unity MonoBehaviour wrapper to invoke INetworkClient methods.
	/// </summary>
	public class NetworkClient : MonoBehaviour
	{
		[SerializeField] private string gameName;
		[SerializeField] private string gameVersion;
		[SerializeField] private bool useLobby;
		[SerializeField] private bool useLocal;
		[SerializeField] private NetworkUrlSettings urlSettings_Local;
		[SerializeField] private NetworkUrlSettings urlSettings_Remote;
		[SerializeField] private CustomMatchmakingData[] customData;

		private static NetworkClient instance;
		private static INetworkClient networkClient;
		private static IAsyncClient asyncClient;
		private static string playerId;
		private static string playerRegion = "Default";
		private static string nickname;
		private static string localTestToken;

		private const string consonantsUpper = "BCDFGHJKLMNPQRSTVWXZ";
		private const string consonantsLower = "bcdfghjklmnpqrstvwxz";
		private const string vowels = "aeiouy";
		private const string AUTH_INFO_KEY = "AuthInfo";

		private string nicknameKey = "Nickname";
		private UserInfo userInfo;

		public static string MyId => playerId;
		public static bool IsConnected => networkClient.IsConnected;
		public static PlayerInfo MyInfo => networkClient.MyInfo;
		public static MatchInfo MatchInfo => networkClient.MatchInfo;
		public static StateInfo StateInfo => networkClient.StateInfo;

		public static Dictionary<string, string> GameSettings => networkClient.GameSettings;

		private void Awake ()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
			{
				Destroy(this);
				return;
			}
			
			var urls = urlSettings_Remote;
#if UNITY_EDITOR
			if (useLocal)
				urls = urlSettings_Local;
#endif
			networkClient = new HttpFunctionsNetworkClient(new UnityWebRequestComnunicator(this), urls.GetUrls());
			asyncClient = networkClient as IAsyncClient;
			DontDestroyOnLoad(this);
#if UNITY_EDITOR
			nicknameKey += $"-{GetLocalDebugToken()}";
#endif
			nickname = Storage.Load(nicknameKey, "");
			if (string.IsNullOrEmpty(nickname))
			{
				nickname = "Guest-" + RandomName(6);
				SaveNicknameLocally(nickname);
			}
		}

		// ████████████████████████████████████████████ P U B L I C ████████████████████████████████████████████

		public static void SetNickname (string nick)
		{
			SaveNicknameLocally(nick);
			SendNicknameToServer(nick);
		}

		public static void SetPlayerData (Dictionary<string, string> data, Action<PlayerInfo> onSuccess, Action<Response> onFailure)
		{
			networkClient.SetPlayerData(data, (success) => onSuccess?.Invoke((PlayerInfo)success), (failure) => onFailure?.Invoke((Response)failure));
		}

		/// <summary>
		/// Invokes the connect method on the Network interface.
		/// </summary>
		/// <param screenName="onSuccess"> True if it's new user </param>
		/// <param screenName="onFailure"> <typeparamref screenName="NetworkError"/> with info on what happened. </param>
		public static void Connect (Action<bool> onSuccess, Action<NetworkError> onFailure)
		{
			string identifier;
			bool hasAuth = Storage.TryLoad(AUTH_INFO_KEY, "", out string info);
			string deviceId = GetDeviceIdentifier();
			if (hasAuth)
			{
				instance.userInfo = JsonConvert.DeserializeObject<UserInfo>(info);
				identifier = instance.userInfo.Email;
			}
			else
				identifier = deviceId;

			Logger.Log("Connecting with identifier " + identifier);

			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected });
				return;
			}

			LoginRequest request = new LoginRequest
			{
				Identifier = identifier,
				DeviceId = deviceId,
				GameId = instance.gameName,
				Nickname = nickname,
				Region = playerRegion
			};
			if (hasAuth)
				request.IdToken = instance.userInfo.IdToken;

			// Invoke network
			networkClient.Connect(request,
				(success) =>
				{
					LoginResponse response = (LoginResponse)success;
					playerId = response.PlayerId;
					SaveNicknameLocally(response.MyInfo.Nickname);
					onSuccess?.Invoke(response.IsAuthenticated);
				},
				(failure) =>
				{
					// Player is not connected to the internet
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		public static void FindMatch (Action<string> onSuccess, Action<NetworkError> onFailure)
		{
			FindMatch(null, onSuccess, onFailure);
		}

		/// <summary>
		/// Tries to find a match.
		/// </summary>
		/// <param screenName="onSuccess"> String with the word "Success". Poll with 'GetMatch' to retrieve information on the match whenever it is ready. </param>
		/// <param screenName="onFailure"> <typeparamref screenName="NetworkError"/> with info on what happened. </param>
		public static void FindMatch (Dictionary<string, string> customData, Action<string> onSuccess, Action<NetworkError> onFailure)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected });
				return;
			}

			if (string.IsNullOrEmpty(playerId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
				return;
			}
			if (instance.customData != null && instance.customData.Length > 0)
			{
				if (customData == null)
					customData = new Dictionary<string, string>();
				foreach (var item in instance.customData)
				{
					customData.Add(item.Key, item.Value);
				}
			}
			networkClient.FindMatch(
				new FindMatchRequest
				{
					GameId = instance.gameName,
					PlayerId = playerId,
					Region = playerRegion,
					UseLobby = instance.useLobby,
					CustomData = customData
				},
				(success) =>
				{
					onSuccess?.Invoke("Success");
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		/// <summary>
		/// Poll to get info on a match if a FindMatch was called before. Or get info on current active match.
		/// </summary>
		/// <param name="onSuccess"> <typeparamref screenName="MatchInfo"/> object with info on the match. </param>
		/// <param name="onFailure"> <typeparamref screenName="NetworkError"/> with info on what happened. </param>
		public static void GetMatch (string alias, Action<MatchInfo> onSuccess, Action<NetworkError> onFailure)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected });
				return;
			}

			if (string.IsNullOrEmpty(playerId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
				return;
			}

			networkClient.GetMatch(
				new MatchRequest
				{
					PlayerId = playerId,
					MatchId = networkClient.MatchInfo?.MatchId,
					GameId = instance.gameName,
					Region = playerRegion,
					Alias = alias
				},
				(success) =>
				{
					MatchInfo matchInfo = (MatchInfo)success;
					onSuccess?.Invoke(matchInfo);
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		/// <summary>
		/// Leaves current match, or stops looking for a match if not matched yet.
		/// </summary>
		/// <param name="onSuccess"></param>
		/// <param name="onFailure"></param>
		public static void LeaveMatch (Action<string> onSuccess, Action<NetworkError> onFailure)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected });
				return;
			}

			if (string.IsNullOrEmpty(playerId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
				return;
			}
			networkClient.LeaveMatch(
				new MatchRequest 
				{ 
					GameId = instance.gameName, 
					Region = playerRegion,
					PlayerId = playerId,
					MatchId = networkClient.MatchInfo?.MatchId ?? ""
				},
				(success) =>
				{
					onSuccess?.Invoke("Success Leaving Match");
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		/// <summary>
		/// Sends an action performed by the player. The action will be validated by the backend service.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="onSuccess"></param>
		/// <param name="onFailure"></param>
		public static void SendAction (ActionInfo action, Action<string> onSuccess, Action<NetworkError> onFailure)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected });
				return;
			}
			if (MatchInfo == null || string.IsNullOrEmpty(MatchInfo.MatchId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotAvailable, Message = "Does not have a match to send action to." });
				return;
			}

			ActionRequest request = new ActionRequest
			{
				PlayerId = playerId,
				MatchId = MatchInfo?.MatchId,
				Action = action
			};
			networkClient.SendAction(request,
				(success) =>
				{
					onSuccess?.Invoke("Success");
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		/// <summary>
		/// Get state on the current match. The info presented in StateInfo depends on the rules defined by the backend code.
		/// </summary>
		/// <param name="onSuccess"></param>
		/// <param name="onFailure"></param>
		public static void GetMatchState (Action<StateInfo> onSuccess, Action<NetworkError> onFailure)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected });
				return;
			}

			if (string.IsNullOrEmpty(playerId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
				return;
			}

			int lastHash = StateInfo?.Hash ?? 0;
			StateRequest request = new StateRequest
			{
				PlayerId = playerId,
				MatchId = MatchInfo?.MatchId,
				LastHash = lastHash
			};
			networkClient.GetMatchState(request,
				(success) =>
				{
					StateInfo state = (StateInfo)success;
					onSuccess?.Invoke(state);
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		public static void StartMatch (Action<string> onSuccess, Action<NetworkError> onFailure)
		{
			SendAction(new ActionInfo { PublicChanges = new() { { "StartMatch", "" } } }, onSuccess, onFailure);
		}

		public static void GetData (string key, string defaultValue, Action<string> onSuccess, Action<NetworkError> onFailure)
		{
			networkClient.GetData(key, defaultValue,
				(success) =>
				{
					onSuccess?.Invoke((string)success);
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		public static void SetData (string key, string value, Action<string> onSuccess, Action<NetworkError> onFailure)
		{
			networkClient.SetData(key, value,
				(success) =>
				{
					onSuccess?.Invoke((string)success);
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		public static void GetBatchData (string query, Action<Dictionary<string, string>> onSuccess, Action<NetworkError> onFailure)
		{
			networkClient.GetBatchData(query,
				(success) =>
				{
					onSuccess?.Invoke((Dictionary<string, string>)success);
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

        public static void AddAsyncObject (string type, AsyncObjectInfo info, Action<string> onSuccess, Action<NetworkError> onFailure)
		{
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected });
                return;
            }
            if (string.IsNullOrEmpty(playerId))
            {
                onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
                return;
            }
            if (info == null)
            {
                onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is null." });
                return;
            }
            info.Author = nickname;
            AddAsyncObjectRequest request = new AddAsyncObjectRequest
            {
                Type = type,
                PlayerId = playerId,
                Info = info
            };
            asyncClient.AddAsyncObject(request,
                (success) =>
                {
                    onSuccess?.Invoke((string)success);
                },
                (failure) =>
                {
                    onFailure?.Invoke((NetworkError)failure);
                });
        }

        public static void GetAsyncObjects (string type, string id, int quantity, Action<AsyncObjectInfo[]> onSuccess, Action<NetworkError> onFailure)
		{
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected });
                return;
            }
            if (string.IsNullOrEmpty(playerId))
            {
                onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
                return;
            }
            if (string.IsNullOrEmpty(type))
            {
                onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.WrongParameters, Message = "Parameter is null." });
                return;
            }
            AsyncObjectRequest request = new AsyncObjectRequest
            {
                Type = type,
                Quantity = quantity,
                Id = id
            };
            asyncClient.GetAsyncObjects(request,
                (success) =>
                {
                    AsyncObjectInfo[] objs = (AsyncObjectInfo[])success;
                    onSuccess?.Invoke(objs);
                },
                (failure) =>
                {
                    onFailure?.Invoke((NetworkError)failure);
                });
        }

        // ████████████████████████████████████████████ P R I V A T E ████████████████████████████████████████████

        private static string GetDeviceIdentifier ()
		{
			string deviceId;
#if UNITY_WEBGL
			deviceId = GetLocalIdentifier();
#else
			deviceId = SystemInfo.deviceUniqueIdentifier;
			if (deviceId == SystemInfo.unsupportedIdentifier)
				deviceId = GetLocalIdentifier();
#endif
#if UNITY_EDITOR
			return $"{deviceId}-{GetLocalDebugToken()}";
#else
			return deviceId;
#endif

			string GetLocalIdentifier ()
			{
				Logger.Log("Getting a local unique identifier");
				string result = Storage.Load("LocalUniqueIdentifier", "");
				if (string.IsNullOrEmpty(result))
				{
					result = Guid.NewGuid().ToString();
					Storage.Save("LocalUniqueIdentifier", result);
				}
				return result;
			}
		}

		// ████████████████████████████████████████████ P R I V A T E ████████████████████████████████████████████

		private static string GetLocalDebugToken ()
		{
			// Local test token
			localTestToken = "editor";
#if PARREL_SYNC && UNITY_EDITOR
			string cloneSuffix = ParrelSync.ClonesManager.GetArgument();
			if (!string.IsNullOrEmpty(cloneSuffix))
				localTestToken = cloneSuffix;
#endif
			return localTestToken;
		}

		private static string RandomName (int length)
		{
			string result = "";
			for (int i = 0; i < length; i++)
			{
				if (i == 0)
					result += consonantsUpper[Random.Range(0, consonantsUpper.Length)];
				else if (i % 2 == 0)
					result += consonantsLower[Random.Range(0, consonantsLower.Length)];
				else
					result += vowels[Random.Range(0, vowels.Length)];
			}
			return result;
		}

		private static void SaveNicknameLocally (string nick)
		{
			nickname = nick;
			Storage.Save(instance.nicknameKey, nick);
		}

		private static void SendNicknameToServer (string nick)
		{
			if (IsConnected)
				networkClient.SetNickname(nick);
		}

		internal static void SetAuthentication (UserInfo userInfo)
		{
			instance.userInfo = userInfo;
			if (userInfo != null)
				Storage.Save(AUTH_INFO_KEY, JsonConvert.SerializeObject(userInfo));
		}
	}

	[System.Serializable]
	public class CustomMatchmakingData
	{
		public string Key;
		public string Value;
	}
}

#endif