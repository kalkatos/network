using System;
using Kalkatos.Network.Specific;
using Kalkatos.Network.Model;
using UnityEngine;
using Kalkatos.FunctionsGame.Models;

namespace Kalkatos.Network.Unity
{
	public class NetworkClient : MonoBehaviour
	{
		private static NetworkClient instance;
		private static INetworkClient networkClient = new AzureFunctionsNetworkClient();
		private static string playerId;
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
		}

		private void OnDestroy ()
		{
			Storage.Save("LocalTester" + localTestToken, 0);
		}

		/// <summary>
		/// Invokes the connect method on the Network interface.
		/// </summary>
		/// <param screenName="onSuccess"> True if it's new user </param>
		/// <param screenName="onFailure"> <typeparamref screenName="NetworkError"/> with info on what happened. </param>
		public static void Connect (Action<bool> onSuccess, Action<NetworkError> onFailure)
		{
			string deviceId = SystemInfo.deviceUniqueIdentifier;

			// Local test token
			localTestToken = "";
			for (int i = 0; i < 10; i++)
			{
				if (Storage.Load("LocalTester" + i, 0) == 0)
				{
					Storage.Save("LocalTester" + i, 1);
					if (i > 0)
						localTestToken = i.ToString();
					break;
				}
			}

			// Invoke network
			networkClient.Connect(deviceId + localTestToken,
				(success) =>
				{
					LoginResponse response = (LoginResponse)success;
					playerId = response.PlayerId;
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
			networkClient.FindMatch(playerId,
				(success) =>
				{
					onSuccess?.Invoke("Success");
				},
				(failure) =>
				{
					onFailure?.Invoke((NetworkError)failure);
				});
		}
	}
}