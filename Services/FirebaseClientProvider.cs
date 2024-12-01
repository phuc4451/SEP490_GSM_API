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
				//foreach (var header in _httpContextAccessor.HttpContext.Request.Headers)
				//{
				//	Console.WriteLine($"{header.Key}: {header.Value}");
				//}

				// Retrieve Firebase token from custom header
				var firebaseToken = _httpContextAccessor.HttpContext?.Request.Headers["Firebase-Token"].FirstOrDefault();

				//var idToken = _httpContextAccessor.HttpContext?.Session.GetString("FirebaseIdToken");

				_firebaseClient = !string.IsNullOrEmpty(firebaseToken)
					? new FirebaseClient(_firebaseBaseUrl, new FirebaseOptions
					{
						AuthTokenAsyncFactory = () => Task.FromResult(firebaseToken)
					})
					: new FirebaseClient(_firebaseBaseUrl); // No token if session is null
			}

			return _firebaseClient;
		}
	}

}
