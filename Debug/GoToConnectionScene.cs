using Kalkatos.UnityGame.Signals;
using UnityEngine;

namespace Kalkatos.Network.Unity
{
	public class GoToConnectionScene : MonoBehaviour
    {
        [SerializeField] private ScreenSignal connectionScene;

        void Awake ()
        {
            if (!NetworkClient.IsConnected)
                connectionScene?.Emit();
		}
    }
}