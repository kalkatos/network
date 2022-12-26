namespace Kalkatos.FunctionsGame.Models
{
	public class LoginRequest
	{
		public string DeviceId;
	}

	public class LoginResponse
	{
		public bool IsNewUser;
		public string SessionKey;
	}
}
