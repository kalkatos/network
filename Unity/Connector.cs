using System;
using UnityEngine;
using Kalkatos.Network.Model;
using Kalkatos.Network.Unity;
using UnityEngine.Events;

namespace Kalkatos.Network.Unity
{
	public class Connector : MonoBehaviour
    {
		[SerializeField] private UnityEvent onConnected;
		[SerializeField] private UnityEvent onNotConnectedError;

		private void Start ()
        {
			if (!NetworkClient.IsConnected)
				TryConnectionAsync();
        }

        private void TryConnectionAsync ()
        {
			Logger.Log("Trying connection...");
			NetworkClient.Connect(
				(success) =>
				{
					onConnected?.Invoke();
					Storage.Save("IsNewUser", success ? 1 : 0);
					Logger.Log("Connected Successfully!");
				},
				(failure) =>
				{
					if (failure.Tag == NetworkErrorTag.NotConnected)
						onNotConnectedError.Invoke();
					Logger.Log("Connection Error: " + failure.Message);
				});
		}
    }
}
