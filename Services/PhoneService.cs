using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;

namespace Alpha_API.Services
{
	public class PhoneService
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		public PhoneService(FirebaseAuth firebaseAuth, FirebaseClientProvider firebaseClientProvider, FirebaseClient firebaseClient)
		{
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		//public User GetUserIdByPhone(string phone)
		//{
		//	_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
		//	await _firebaseClient.Child("users").Child()


		//}
	}
}
