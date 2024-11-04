using Firebase.Database;

namespace Alpha_API.Services
{
	public class FirebaseClientProvider
	{
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly string _firebaseBaseUrl;
		private FirebaseClient _firebaseClient;

		public FirebaseClientProvider(IHttpContextAccessor httpContextAccessor, string firebaseBaseUrl)
		{
			_httpContextAccessor = httpContextAccessor;
			_firebaseBaseUrl = firebaseBaseUrl;
		}

		public FirebaseClient GetFirebaseClient()
		{
			// Only initialize FirebaseClient when it's actually requested
			if (_firebaseClient == null)
			{
				var idToken = _httpContextAccessor.HttpContext?.Session.GetString("FirebaseIdToken");

				_firebaseClient = !string.IsNullOrEmpty(idToken)
					? new FirebaseClient(_firebaseBaseUrl, new FirebaseOptions
					{
						AuthTokenAsyncFactory = () => Task.FromResult(idToken)
					})
					: new FirebaseClient(_firebaseBaseUrl); // No token if session is null
			}

			return _firebaseClient;
		}
	}

}
