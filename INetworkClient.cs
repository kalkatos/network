using System;
using Kalkatos.Network.Model;

namespace Kalkatos.Network
{
	public interface INetworkClient
	{
		event Action<byte, object> OnEventReceived;

		bool IsConnected { get; }
		bool IsInRoom { get; }
		PlayerInfo[] Players { get; }
		PlayerInfo MyInfo { get; }
		RoomInfo RoomInfo { get; }

		void Connect (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void FindMatch (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void LeaveMatch (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void GetMatch (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void SetNickname (string nickname);
		void Get (byte key, object parameter, Action<object> onSuccess, Action<object> onFailure);
		void Post (byte key, object parameter, Action<object> onSuccess, Action<object> onFailure);
	}
}