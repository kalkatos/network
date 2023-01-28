using UnityEngine;
using UnityEngine.Events;

namespace Kalkatos.Network.Unity
{
	[CreateAssetMenu(fileName = "NetworkEventDispatcher", menuName = "Network/Event Dispatcher")]
	public class NetworkEventDispatcher : ScriptableObject
	{
		public UnityEvent OnConnectSuccess;
		public UnityEvent OnConnectFailure;
		public UnityEvent OnFindMatchSuccess;
		public UnityEvent OnFindMatchFailure;
		public UnityEvent OnGetMatchSuccess;
		public UnityEvent OnGetMatchFailure;
		public UnityEvent OnLeaveMatchSuccess;
		public UnityEvent OnLeaveMatchFailure;

		public void SetNickname (string nick)
		{
			NetworkClient.SetNickname(nick);
		}

		public void Connect ()
		{
			NetworkClient.Connect((success) => OnConnectSuccess?.Invoke(), (failure) => OnConnectFailure?.Invoke());
		}

		public void FindMatch ()
		{
			NetworkClient.FindMatch((success) => OnFindMatchSuccess?.Invoke(), (failure) => OnFindMatchFailure?.Invoke());
		}

		public void GetMatch ()
		{
			NetworkClient.GetMatch((success) => OnGetMatchSuccess?.Invoke(), (failure) => OnGetMatchFailure?.Invoke());
		}

		public void LeaveMatch ()
		{
			NetworkClient.LeaveMatch((success) => OnLeaveMatchSuccess?.Invoke(), (failure) => OnLeaveMatchFailure?.Invoke());
		}
	}
}