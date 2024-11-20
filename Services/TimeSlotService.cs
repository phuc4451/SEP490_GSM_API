using Alpha_API.Models;
using Firebase.Database;

namespace Alpha_API.Services
{
	public class TimeSlotService
	{
		private readonly FirebaseClient _firebaseClient;
		private Dictionary<int, string> _timeSlots;

		public TimeSlotService(FirebaseClient firebaseClient)
		{
			_firebaseClient = firebaseClient;
		}

		public async Task LoadTimeSlotsAsync()
		{
			var timeSlots = await _firebaseClient
				.Child("TimeSlots")
				.OnceAsync<TimeSlots>();

			_timeSlots = timeSlots.ToDictionary(
				ts => ts.Object.TimeSlotId,
				ts => ts.Object.TimeSlot
			);
		}

		public string GetTimeSlot(int timeSlotId)
		{
			if (_timeSlots.TryGetValue(timeSlotId, out var timeSlot))
			{
				return timeSlot;
			}

			throw new KeyNotFoundException($"TimeSlotId {timeSlotId} not found.");
		}

		public Dictionary<int, string> GetAllTimeSlots()
		{
			return _timeSlots;
		}
	}

}
