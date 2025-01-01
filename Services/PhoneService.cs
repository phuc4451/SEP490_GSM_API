using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Alpha_API.Wrapper.Interfaces;

namespace Alpha_API.Services
{
	public class PhoneService
	{
		private readonly IFirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		public PhoneService(IFirebaseAuth firebaseAuth, FirebaseClientProvider firebaseClientProvider, FirebaseClient firebaseClient)
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
