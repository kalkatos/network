// (c) 2023 Alex Kalkatos
// This code is licensed under MIT license (see LICENSE.txt for details)

#if UNITY_2018_1_OR_NEWER

using Kalkatos.Network.Model;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Kalkatos.Network.Unity
{
	public class AuthClientUnityWrapper : MonoBehaviour
	{
		private static AuthClientUnityWrapper instance;

		[Header("Config")]
		[SerializeField] private AuthUrls prodUrls;
		[SerializeField] private AuthUrls localUrls;
		[SerializeField] private bool useLocal;

		[FoldoutGroup("Events")]
		public UnityEvent<UserInfo> OnAuthSuccess;
		[FoldoutGroup("Events")]
		public UnityEvent<string> OnAuthFailed;

		private AuthClient client;
		private UserInfo userInfo;

		private enum RequestStatus { Idle, Processing, Ended }

		private void Awake ()
		{
			instance = this;

			if (useLocal)
				client = new AuthClient(localUrls, NetworkClient.MyId, NetworkClient.GetDeviceIdentifier());
			else
				client = new AuthClient(prodUrls, NetworkClient.MyId, NetworkClient.GetDeviceIdentifier());
		}

		private void OnDestroy ()
		{
			client.Dispose();
		}

		public static void ExecuteAuthStatic ()
		{
			instance.ExecuteAuth();
		}

		public void ExecuteAuth ()
		{
			StartCoroutine(RequestUserData());
		}

		private IEnumerator RequestUserData ()
		{
			string authUrl = "";
			_ = client.RequestAuthUrl(
				success => authUrl = success,
				() => authUrl = "Error");
			while (string.IsNullOrEmpty(authUrl))
				yield return null;
			if (authUrl == "Error")
			{
				Logger.LogError("Error getting URL");
				yield break;
			}
			Application.OpenURL(authUrl);

			yield return new WaitForSeconds(7f);
			userInfo = null;
			string log = "";
			int attempt = 0;
			int maxAttempts = 5;
			do
			{
				bool hasReturned = false;
				_ = client.RequestUserInfo(
					info =>
					{
						hasReturned = true;
						userInfo = info;
					},
					failure =>
					{
						hasReturned = true;
						log = failure;
					});
				while (!hasReturned)
					yield return null;
				yield return new WaitForSeconds(Random.Range(2.5f, 3.5f));
				attempt++;
			} while (userInfo == null && attempt <= maxAttempts);
			if (userInfo != null)
			{
				Logger.Log($"Received user info: {JsonConvert.SerializeObject(userInfo)}");
				OnAuthSuccess?.Invoke(userInfo);
				NetworkClient.SetAuthentication(userInfo);
			}
			else
			{
				string msg = $"Error loading user info: {log}";
				Logger.LogError(msg);
				OnAuthFailed?.Invoke(msg);
			}
		}
	}
}

#endif
