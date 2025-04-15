// (c) 2023 Alex Kalkatos
// This code is licensed under MIT license (see LICENSE.txt for details)

using System;
using System.Collections;
using System.Collections.Generic;
using Kalkatos.Network.Model;

namespace Kalkatos.Network
{
	/// <summary>
	/// Base interface for sending and receiving data from a server.
	/// </summary>
	public interface INetworkClient
	{
		bool IsConnected { get; }
		bool IsInRoom { get; }
		PlayerInfo[] Players { get; }
		PlayerInfo MyInfo { get; }
		MatchInfo MatchInfo { get; }
		StateInfo StateInfo { get; }
		Dictionary<string, string> GameSettings { get; }

		void Connect (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void FindMatch (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void LeaveMatch (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void GetMatch (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void SetNickname (string nickname);
		void SetPlayerData (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void SendAction (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void GetMatchState (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void GetData (string key, string defaultValue, Action<object> onSuccess, Action<object> onFailure);
		void SetData (string key, string value, Action<object> onSuccess, Action<object> onFailure);
		void GetBatchData (string query, Action<object> onSuccess, Action<object> onFailure);
	}

	public interface IAsyncClient
	{
		void AddAsyncObject (object parameter, Action<object> onSuccess, Action<object> onFailure);
		void GetAsyncObjects (object parameter, Action<object> onSuccess, Action<object> onFailure);
	}
}