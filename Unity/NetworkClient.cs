using System;
using System.Collections;
using System.Collections.Generic;
using Kalkatos.Network.Model;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kalkatos.Network.Unity
{
	public class NetworkClient : MonoBehaviour
	{
		[SerializeField] private string gameId;

		private static NetworkClient instance;
		private static INetworkClient networkClient = new AzureFunctionsNetworkClient();
		private static string playerId;
		private static string playerRegion;
		private static string nickname;
		private static bool canLeaveMatch = true;
		private static string localTestToken;

		private const string consonantsUpper = "BCDFGHJKLMNPQRSTVWXZ";
		private const string consonantsLower = "bcdfghjklmnpqrstvwxz";
		private const string vowels = "aeiouy";

		public static bool IsConnected => networkClient.IsConnected;
		public static PlayerInfo MyInfo => networkClient.MyInfo;
		public static MatchInfo MatchInfo => networkClient.MatchInfo;
		public static StateInfo StateInfo => networkClient.StateInfo;

		private void Awake ()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
			{
				Destroy(this);
				return;
			}

			DontDestroyOnLoad(this);
#if UNITY_EDITOR
			Storage.SetFileName($"{GetDeviceIdentifier()}.json");
#endif
			nickname = Storage.Load("Nickname", "");
			if (string.IsNullOrEmpty(nickname))
			{
				nickname = "Guest-" + RandomName(6);
				SaveNicknameLocally(nickname);
			}
		}

		// =========================================================  P U B L I C ==============================================================

		public static void SetNickname (string nick)
		{
			SaveNicknameLocally(nick);
			SendNicknameToServer(nick);
		}

		public static void SetPlayerData (Dictionary<string, string> data)
		{
			networkClient.SetPlayerData(data, null, null);
		}

		/// <summary>
		/// Invokes the connect method on the Network interface.
		/// </summary>
		/// <param screenName="onSuccess"> True if it's new user </param>
		/// <param screenName="onFailure"> <typeparamref screenName="NetworkError"/> with info on what happened. </param>
		public static void Connect (Action<bool> onSuccess, Action<NetworkError> onFailure)
		{
			string deviceId = GetDeviceIdentifier();
			Logger.Log("Connecting with identifier " + deviceId);

			// TODO Get player region
			playerRegion = "US";

			// Invoke network
			networkClient.Connect(new LoginRequest { Identifier = deviceId, GameId = instance.gameId, Region = playerRegion, Nickname = nickname },
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

		/// <summary>
		/// Tries to find a match.
		/// </summary>
		/// <param screenName="onSuccess"> String with the matchmaking ticket. </param>
		/// <param screenName="onFailure"> <typeparamref screenName="NetworkError"/> with info on what happened. </param>
		public static void FindMatch (Action<string> onSuccess, Action<NetworkError> onFailure)
		{
			if (string.IsNullOrEmpty(playerId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
				return;
			}
			networkClient.FindMatch(null,
				(success) =>
				{
					onSuccess?.Invoke("Success");
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		public static void GetMatch (Action<MatchInfo> onSuccess, Action<NetworkError> onFailure)
		{
			if (string.IsNullOrEmpty(playerId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
				return;
			}

			MatchRequest request = new MatchRequest
			{
				PlayerId = playerId,
				MatchId = networkClient.MatchInfo?.MatchId,
			};
			networkClient.GetMatch(request,
				(success) =>
				{
					MatchInfo matchInfo = (MatchInfo)success;
					onSuccess?.Invoke(matchInfo);
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});

			//MatchInfo matchInfo = networkClient.MatchInfo;
			//if (matchInfo == null || string.IsNullOrEmpty(matchInfo.MatchId))
			//	onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotAvailable, Message = "Match is not available yet." });
			//else
			//{
			//	onSuccess?.Invoke(matchInfo);
			//	OnGetMatch?.Invoke(matchInfo);
			//}
			canLeaveMatch = true;
		}

		public static void LeaveMatch (Action<string> onSuccess, Action<NetworkError> onFailure)
		{
			if (string.IsNullOrEmpty(playerId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
				return;
			}
			networkClient.LeaveMatch(null,
				(success) =>
				{
					MatchResponse response = success as MatchResponse;
					if (response.MatchId != null)
					{
						canLeaveMatch = false;
						instance.StartCoroutine(WaitUntilMatchGotToLeave(() => onSuccess?.Invoke("Success Leaving Match")));
					}
					else
					{
						onSuccess?.Invoke("Success Leaving Match");
					}
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}

		public static void SendAction (ActionInfo action, Action<StateInfo> onSuccess, Action<NetworkError> onFailure) 
		{
			ActionRequest request = new ActionRequest
			{
				PlayerId = playerId,
				MatchId = MatchInfo.MatchId,
				Action = action
			};
			networkClient.SendAction(request,
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

		public static void GetMatchState (Action<StateInfo> onSuccess, Action<NetworkError> onFailure)
		{
			int lastHash = StateInfo?.Hash ?? 0;
			StateRequest request = new StateRequest
			{
				PlayerId = playerId,
				MatchId = MatchInfo.MatchId,
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

		// =========================================================  P R I V A T E ==============================================================

		private static string GetDeviceIdentifier ()
		{
			string deviceId = SystemInfo.deviceUniqueIdentifier;
#if UNITY_EDITOR
			// Local test token
			localTestToken = "editor";
#if PARREL_SYNC
			string cloneSuffix = ParrelSync.ClonesManager.GetArgument();
			if (!string.IsNullOrEmpty(cloneSuffix))
				localTestToken = cloneSuffix;
#endif
			deviceId += $"-{localTestToken}";
#endif
			return deviceId;
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
			Storage.Save("Nickname", nick);
		}

		private static void SendNicknameToServer (string nick)
		{
			if (IsConnected)
				networkClient.SetNickname(nick);
		}

		private static IEnumerator WaitUntilMatchGotToLeave (Action callback)
		{
			Logger.Log($"[{nameof(NetworkClient)}] LeaveMatch: Waiting GetMatch to be called at least once before being able to leave match.");
			while (!canLeaveMatch)
				yield return null;
			callback?.Invoke();
		}
	}
}