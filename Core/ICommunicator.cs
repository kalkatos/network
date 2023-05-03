using System.Threading.Tasks;

namespace Kalkatos.Network
{
	public interface ICommunicator
	{
		Task<string> Get (string url);
		Task<string> Post (string url, string message);
	}
}