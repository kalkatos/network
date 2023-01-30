using Kalkatos.Network.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Kalkatos.Network.Unity
{
	[CreateAssetMenu(fileName = "NetworkInfoDispatcher", menuName = "Network/Info Dispatcher")]
	public class NetworkInfoDispatcher : ScriptableObject
	{
		public UnityEvent<string> OnPlayerNicknameChanged;
		public UnityEvent<string> OnOpponentNicknameReceived;
		public UnityEvent<int> OnPlayerAvatarChanged;
		public UnityEvent<int> OnOpponentAvatarReceived;

		public void UpdatePlayerData ()
		{
			PlayerInfo myInfo = NetworkClient.MyInfo;
			if (myInfo != null)
			{
				string nickname = myInfo.Nickname;
				OnPlayerNicknameChanged?.Invoke(nickname);
				Logger.Log($"Player nickname: {nickname}");
				if (myInfo.CustomData != null && myInfo.CustomData.TryGetValue("Avatar", out string value))
				{
					int avatarIndex = int.Parse(value);
					OnPlayerAvatarChanged?.Invoke(avatarIndex);
					Logger.Log($"Player avatar: {avatarIndex}");
				}
			}
		}

		public void UpdateOpponentData ()
		{
			if (NetworkClient.MatchInfo != null)
			{
				foreach (var item in NetworkClient.MatchInfo.Players)
				{
					if (item.Alias != NetworkClient.MyInfo.Alias)
					{
						string opponentNickname = item.Nickname;
						OnOpponentNicknameReceived?.Invoke(opponentNickname);
						Logger.Log($"Received opponent nickname: {opponentNickname}");
						if (item.CustomData != null && item.CustomData.TryGetValue("Avatar", out string value))
						{
							int avatarIndex = int.Parse(value);
							OnOpponentAvatarReceived?.Invoke(avatarIndex);
							Logger.Log($"Opponent avatar: {avatarIndex}");
						}
						break;
					}
				}
			}
		}
	}
}