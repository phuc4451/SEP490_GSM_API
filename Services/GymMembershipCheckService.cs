using Alpha_API.Models;
using Alpha_API.Wrapper.Interfaces;
using Firebase.Database;
using Firebase.Database.Query;

namespace Alpha_API.Services
{
	public class GymMembershipCheckService : IGymMembershipCheckService
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
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var existingGymRegistrations = await _firebaseClient
				.Child("GymRegistrations")
				.OrderBy("userId")
				.EqualTo(userId)
				.OnceAsync<GymRegistration>();

			var hasActiveMembership = existingGymRegistrations
				.Any(ex => ex.Object.IsActive &&
				(ex.Object.EndDate >= DateTime.Now && ex.Object.SessionLeft > 0));

			if (!hasActiveMembership)
			{
				return false;
			}

			return true;
		}

        public async Task<(bool, bool)> CheckGymMembershipEndDate(string userId, DateOnly endDate)
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();
            var existingGymRegistrations = await _firebaseClient
                .Child("GymRegistrations")
                .OrderBy("userId")
                .EqualTo(userId)
                .OnceAsync<GymRegistration>();

            var hasValidMembership = existingGymRegistrations
                .Any(ex => ex.Object.IsActive &&
                (DateOnly.FromDateTime(ex.Object.EndDate) >= endDate && ex.Object.SessionLeft > 0));

            var hasActiveMembership = existingGymRegistrations
                .Any(ex => ex.Object.IsActive &&
                (ex.Object.EndDate >= DateTime.Now && ex.Object.SessionLeft > 0));

            if (!hasActiveMembership)
            {
                return (false, false);
            }

            if (!hasValidMembership && hasActiveMembership)
            {
                return (true, false);
            }

            return (true, true);
        }
    }
}
