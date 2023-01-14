using System;
using System.Collections;
using Kalkatos.Network.Specific;
using Kalkatos.Network.Model;
using UnityEngine;
using Kalkatos.FunctionsGame.Models;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;

namespace Kalkatos.Network.Unity
{
	public class NetworkClient : MonoBehaviour
	{
		private static NetworkClient instance;
		private static INetworkClient networkClient = new AzureFunctionsNetworkClient();
		private static string playerId;
		private static string playerRegion;
		private static string nickname;
		private static string localTestToken;

		public static bool IsConnected => networkClient.IsConnected;

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
			nickname = Storage.Load("Nickname", "");
			if (string.IsNullOrEmpty(nickname))
				nickname = "Guest-" + RandomName(6);
		}

		private void OnDestroy ()
		{
			Storage.Save("LocalTester" + localTestToken, 0);
		}

		private static IEnumerator FetchRoomCoroutine (Action<RoomInfo> onSuccess, Action<NetworkError> onFailure, float timeout)
		{
			float startTime = Time.time;
			bool isTimedOut = false;
			bool isValidRoom = !string.IsNullOrEmpty(networkClient.RoomInfo.RoomId);
			while (!isValidRoom && !isTimedOut)
			{
				isValidRoom = !string.IsNullOrEmpty(networkClient.RoomInfo.RoomId);
				isTimedOut = Time.time - startTime > timeout;
				yield return null;
			}
			if (isValidRoom)
				onSuccess?.Invoke(networkClient.RoomInfo);
			else
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotFound, Message = "Couldn't find any room." });
		}

		private static string RandomName (int length)
		{
			string consonantsUpper = "BCDFGHJKLMNPQRSTVWXZ";
			string consonantsLower = "bcdfghjklmnpqrstvwxz";
			string vowels = "aeiouy";
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

		public static void SetNickname (string nick)
		{
			nickname = nick;
			if (IsConnected)
				networkClient.SetNickname(nick);
			Storage.Save("Nickname", nick);
		}

		/// <summary>
		/// Invokes the connect method on the Network interface.
		/// </summary>
		/// <param screenName="onSuccess"> True if it's new user </param>
		/// <param screenName="onFailure"> <typeparamref screenName="NetworkError"/> with info on what happened. </param>
		public static void Connect (Action<bool> onSuccess, Action<NetworkError> onFailure)
		{
			string deviceId = SystemInfo.deviceUniqueIdentifier;

			// TODO Get player region
			playerRegion = "US";

			// TODO Ask for player nickname or get it somewhere
			nickname = Storage.Load("Nickname", "Guest-" + RandomName(6));

			// Local test token
			localTestToken = "";
			if (Storage.Load("LocalTester", 0) > 0)
			{
				for (int i = 1; i < 10; i++)
				{
					if (Storage.Load("LocalTester" + i, 0) == 0)
					{
						Storage.Save("LocalTester" + i, 1);
						if (i > 0)
							localTestToken = i.ToString();
						break;
					}
				}
			}
			else
				Storage.Save("LocalTester", 1);

			// Invoke network
			networkClient.Connect(new PlayerConnectInfo { Identifier = deviceId + localTestToken, Region = playerRegion, Nickname = nickname },
				(success) =>
				{
					LoginResponse response = (LoginResponse)success;
					playerId = response.PlayerId;
					nickname = response.SavedNickname;
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

		public static void FetchRoomInfo (Action<RoomInfo> onSuccess, Action<NetworkError> onFailure, float timeout = 10.0f)
		{
			if (string.IsNullOrEmpty(playerId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
				return;
			}

			RoomInfo roomInfo = networkClient.RoomInfo;
			if (string.IsNullOrEmpty(roomInfo.RoomId))
				instance.StartCoroutine(FetchRoomCoroutine(onSuccess, onFailure, timeout));
			else
				onSuccess?.Invoke(roomInfo);
		}
	}
}