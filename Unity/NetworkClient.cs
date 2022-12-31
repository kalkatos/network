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

		/// <summary>
		/// Invokes the connect method on the Network interface.
		/// </summary>
		/// <param name="onSuccess"> True if it's new user </param>
		/// <param name="onFailure"> <typeparamref name="NetworkError"/> with info on what happened. </param>
		public static void Connect (Action<bool> onSuccess, Action<NetworkError> onFailure)
		{
			string deviceId = SystemInfo.deviceUniqueIdentifier;
			// Invoke network
			_networkClient.Connect(deviceId,
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
		/// <param name="onSuccess"> String with the matchmaking ticket. </param>
		/// <param name="onFailure"> <typeparamref name="NetworkError"/> with info on what happened. </param>
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