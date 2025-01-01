using Alpha_API.Models;
using Alpha_API.Wrapper.Interfaces;
using Firebase.Database;

namespace Alpha_API.Services
{
	public class TimeSlotService : ITimeSlotService
	{
		private readonly FirebaseClient _firebaseClient;
		private Dictionary<string, string> _timeSlots;

		public TimeSlotService(FirebaseClient firebaseClient)
		{
			_firebaseClient = firebaseClient;
		}

		public async Task LoadTimeSlotsAsync()
		{
			// Fetch the raw JSON response as a List of TimeSlot
			var timeSlots = await _firebaseClient
				.Child("TimeSlots")
				.OnceAsync<TimeSlot>();

			// Filter out null or invalid entries
			_timeSlots = timeSlots
				.Where(ts => ts.Object != null) // Ignore null entries
				.ToDictionary(
					ts => ts.Key, // Use TimeslotId as the key
					ts => ts.Object.Time // Use TimeSlot as the value
				);
		}

		public string GetTimeSlot(string timeSlotId)
		{
			if (_timeSlots.TryGetValue(timeSlotId, out var timeSlot))
			{
				return timeSlot;
			}

			throw new KeyNotFoundException($"TimeSlotId {timeSlotId} not found.");
		}

		public Dictionary<string, string> GetAllTimeSlots()
		{
			return _timeSlots;
		}
	}
}
