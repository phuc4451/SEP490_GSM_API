using Alpha_API.Models;
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

		public async Task<IEnumerable<Staff>> GetStaffAsync()
		{
			try
			{
				// Initialize Firebase client (assuming it's being injected)
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				// Retrieve shifts in Firebase
				var staffsSnapshot = await _firebaseClient.Child("Staffs").OnceAsync<Staff>();

				// Create a list to store updated shifts with their ShiftId set
				var staffs = staffsSnapshot.Select(staffSnapshot =>
				{
					var staff = staffSnapshot.Object;
					staff.StaffId = staffSnapshot.Key; // Set ShiftId to the Firebase key
					return staff;
				}).ToList();

				return staffs;
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error retrieving staffs: {ex.Message}");
				throw new InvalidOperationException("An error occurred while retrieving the staffs.", ex);
			}
		}
	}
}
