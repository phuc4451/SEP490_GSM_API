using Alpha_API.Models;
using Firebase.Database;
using Firebase.Database.Query;

namespace Alpha_API.Services
{
	public class GymMembershipCheckService
	{
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private FirebaseClient _firebaseClient;
		public GymMembershipCheckService(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		public async Task<bool> CheckGymMembership(string userId)
		{
			var existingGymRegistrations = await _firebaseClient
				.Child("GymRegistrations")
				.OrderBy("userId")
				.EqualTo(userId)
				.OnceAsync<GymRegistration>();

			var hasActiveMembership = existingGymRegistrations
				.Any(ex => ex.Object.IsActive &&
				(ex.Object.EndDate >= DateTime.Now || ex.Object.SessionLeft > 0));

			if (!hasActiveMembership)
			{
				return false;
			}

			return true;
		}
	}
}
