using UnityEngine;

namespace Kalkatos.Network.Unity
{
	[CreateAssetMenu(fileName = "NewActionObject", menuName = "Network/Action Object")]
	public class ActionSO : ScriptableObject
	{
		public string ActionName;
		public string Parameter;

		public void SetActionName (string name)
		{
			ActionName = name;
		}

		public void SetParameter (string param)
		{
			Parameter = param;
		}
	}
}