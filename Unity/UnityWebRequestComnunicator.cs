using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Kalkatos.Network.Unity
{
	public class UnityWebRequestComnunicator : ICommunicator
	{
		private MonoBehaviour mono;

		public UnityWebRequestComnunicator (MonoBehaviour mono)
		{
			this.mono = mono;
		}

		public async Task<string> Get (string url)
		{
			using (UnityWebRequest request = UnityWebRequest.Get(url))
			{
				TaskCompletionSource<bool> taskAwaiter = new TaskCompletionSource<bool>();
				request.downloadHandler = new DownloadHandlerBuffer();
				UnityWebRequestAsyncOperation operation = request.SendWebRequest();
				mono.StartCoroutine(WaitOperationCoroutine(operation, taskAwaiter));
				await taskAwaiter.Task;
				string result = request.downloadHandler.text;
				return result;
			}
		}

		public async Task<string> Post (string url, string message)
		{
			using (UnityWebRequest request = new UnityWebRequest(url))
			{
				request.method = "POST";
				request.SetRequestHeader("Content-Type", "application/json");
				request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(message));
				request.uploadHandler.contentType = "application/json";
				request.downloadHandler = new DownloadHandlerBuffer();
				TaskCompletionSource<bool> taskAwaiter = new TaskCompletionSource<bool>();
				UnityWebRequestAsyncOperation operation = request.SendWebRequest();
				mono.StartCoroutine(WaitOperationCoroutine(operation, taskAwaiter));
				await taskAwaiter.Task;
				string result = request.downloadHandler.text;
				return result;
			}
		}

		private IEnumerator WaitOperationCoroutine (UnityWebRequestAsyncOperation operation, TaskCompletionSource<bool> awaiter)
		{
			yield return operation;
			awaiter.SetResult(true);
		}
	}
}