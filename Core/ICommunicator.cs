using System;

namespace Kalkatos.Network
{
	public interface ICommunicator
	{
		void Get (string url, Action<string> callback);
		void Post (string url, string message, Action<string> callback);
	}
}