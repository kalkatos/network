using System;
using Kalkatos.Network.Specific;
using Kalkatos.Network.Model;
using UnityEngine;
using Kalkatos.FunctionsGame.Models;

namespace Kalkatos.Network.Unity
{
	public class NetworkClient : MonoBehaviour
	{
		private static INetworkClient _networkClient = new AzureFunctionsNetworkClient();
		private static string _playerId;
		private static string _localTestToken;

		private void OnDestroy ()
		{
			Storage.Save("LocalTester" + _localTestToken, 0);
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
			_localTestToken = "";
			for (int i = 0; i < 10; i++)
			{
				if (Storage.Load("LocalTester" + i, 0) == 0)
				{
					Storage.Save("LocalTester" + i, 1);
					if (i > 0)
						_localTestToken = i.ToString();
					break;
				}
			}

			// Invoke network
			_networkClient.Connect(deviceId + _localTestToken,
				(success) =>
				{
					LoginResponse response = (LoginResponse)success;
					_playerId = response.PlayerId;
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
			if (string.IsNullOrEmpty(_playerId))
			{
				onFailure?.Invoke(new NetworkError { Tag = NetworkErrorTag.NotConnected, Message = "Not connected. Connect first." });
				return;
			}
			_networkClient.FindMatch(_playerId,
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