using Microsoft.AspNetCore.Mvc;
using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha_API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CheckInController : ControllerBase
	{
		private readonly TimeSlotService _timeSlotService;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private FirebaseClient _firebaseClient;

		public CheckInController(FirebaseClientProvider firebaseClientProvider, TimeSlotService timeSlotService)
		{
			_firebaseClientProvider = firebaseClientProvider;
			_timeSlotService = timeSlotService;
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
		}

		[HttpPost]
		public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
		{
			if (request?.UserId == null || request.Time == null)
			{
				return BadRequest("Invalid request data.");
			}

			// Cache current time details
			var currentDate = DateOnly.FromDateTime(DateTime.Now);
			var currentTime = TimeOnly.FromDateTime(DateTime.Now);

			// Create and store the check-in
			var newCheckIn = new CheckIn
			{
				UserId = request.UserId,
				Time = request.Time
			};
			await _firebaseClient.Child("CheckIns").PostAsync(newCheckIn);

			// Query schedules for the user within the current date range
			var schedules = (await _firebaseClient
				.Child("Schedules")
				.OnceAsync<Schedule>())
				.Where(schedule =>
					schedule.Object.UserIds.Split(',')
						.Any(userId => userId == request.UserId) &&
					schedule.Object.FirstSlot <= currentDate &&
					schedule.Object.LastSlot >= currentDate)
				.ToList();

			if (!schedules.Any())
			{
				return Ok("No schedules found for the user.");
			}

			// Pre-fetch all slots related to the schedules
			var scheduleKeys = schedules.Select(s => s.Key).ToHashSet();
			var slots = (await _firebaseClient
				.Child("Slots")
				.OnceAsync<Slot>())
				.Where(slot => scheduleKeys.Contains(slot.Object.ScheduleId) && slot.Object.Date == currentDate)
				.ToList();

			if (!slots.Any())
			{
				return Ok("No slots found for today.");
			}

			// Process slots and update attendance
			foreach (var slot in slots)
			{
				var timeRange = _timeSlotService.GetTimeSlot(slot.Object.TimeSlotId).Split('-');
				if (timeRange.Length == 2 &&
					TimeOnly.ParseExact(timeRange[0], "H:mm") <= currentTime &&
					TimeOnly.ParseExact(timeRange[1], "H:mm") >= currentTime)
				{
					await _firebaseClient.Child("Slots").Child(slot.Key).PatchAsync(new
					{
						attended = true
					});
					return Ok("Check-in recorded successfully.");
				}
			}

			return Ok("Check-in recorded successfully.");
		}
	}

	// Models
	public class CheckIn
	{
		public string UserId { get; set; }
		public DateTime? Time { get; set; }
	}

	public class CheckInRequest
	{
		public string UserId { get; set; }
		public DateTime? Time { get; set; }
	}
}
