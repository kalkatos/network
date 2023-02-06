using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Kalkatos.Network.Unity
{
	[CreateAssetMenu(fileName = "NewStatePiece", menuName = "Network/State Piece")]
	public class StatePiece : ScriptableObject
	{
		[SerializeField, FormerlySerializedAs("Key")] private string key;
		[SerializeField, FormerlySerializedAs("Value")] private string value;
		[SerializeField, FormerlySerializedAs("OnKeySet")] private UnityEvent<string> onKeySet;
		[SerializeField, FormerlySerializedAs("OnValueSet")] private UnityEvent<string> onValueSet;

		public string Key => key;
		public string Value => value;

		public void SetKey (string name)
		{
			key = name;
			onKeySet?.Invoke(name);
		}

		public void SetValue (string param)
		{
			value = param;
			onValueSet?.Invoke(param);
		}
	}
}