using Firebase.Database;
using Firebase.Database.Query;

namespace Alpha_API.Services
{
	public class StaffService
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly TimeSlotService _timeSlotService;

		public StaffService(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider, TimeSlotService timeSlotService)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			_timeSlotService = timeSlotService;
		}

		public async Task<string> GetStaffName(string staffId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var name = await _firebaseClient
				.Child("Staffs")
				.Child(staffId)
				.Child("fullName")
				.OnceSingleAsync<string>();

			return name;
		}
	}
}
