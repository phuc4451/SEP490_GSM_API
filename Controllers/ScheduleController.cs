//using Alpha_API.Models;
//using Alpha_API.Services;
//using Firebase.Database;
//using Firebase.Database.Query;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Alpha_API.Controllers
//{
//	[Route("api/[controller]")]
//	[ApiController]
//	public class ScheduleController : ControllerBase
//	{
//		private FirebaseClient _firebaseClient;
//		private readonly FirebaseClientProvider _firebaseClientProvider;

//		public ScheduleController(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider)
//		{
//			_firebaseClient = firebaseClient;
//			_firebaseClientProvider = firebaseClientProvider;
//		}

//		// GET: api/Schedule/User/{userId}
//		[HttpGet("User/{userId}")]
//		public async Task<IActionResult> GetUserSchedules(string userId)
//		{
//			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
//			var schedules = await _firebaseClient
//				.Child("Schedules")
//				.OrderBy("UserId")
//				.EqualTo(userId)
//				.OnceAsync<Schedule>();

//			if (!schedules.Any())
//				return NotFound("No schedules found for this user.");

//			foreach (var schedule in schedules)
//			{
//				schedule.Object.ScheduleId = schedule.Key;
//			}

//			return Ok(schedules.Select(s => s.Object));
//		}

//		// POST: api/Schedule
//		[HttpPost]
//		public async Task<IActionResult> CreateSchedule(string planType, Schedule schedule, List<Slot> sessions)
//		{
//			// Validate sessions based on the plan type
//			var validation = ValidateSessions(planType, sessions);
//			if (!validation.IsValid)
//				return BadRequest(validation.ErrorMessage);

//			// Check if Trainer is fully booked on the days of the new sessions
//			var trainerAvailability = await CheckTrainerAvailability(schedule.TrainerId, sessions);
//			if (!trainerAvailability)
//				return BadRequest("Trainer is fully booked for one or more session dates.");

//			// Generate IDs and save schedule to Firebase
//			schedule.ScheduleId = Guid.NewGuid().ToString();
//			schedule.SessionCount = sessions.Count;
//			schedule.FirstSession = sessions.Min(s => s.Date);
//			schedule.LastSession = sessions.Max(s => s.Date);

//			await _firebaseClient
//				.Child("Schedules")
//				.Child(schedule.ScheduleId)
//				.PutAsync(schedule);

//			foreach (var session in sessions)
//			{
//				session.SlotId = Guid.NewGuid().ToString();
//				session.ScheduleId = schedule.ScheduleId;

//				await _firebaseClient
//					.Child("Sessions")
//					.Child(session.SlotId)
//					.PutAsync(session);
//			}

//			return Ok("Schedule created successfully.");
//		}

//		private (bool IsValid, string ErrorMessage) ValidateSessions(string planType, List<Slot> sessions)
//		{
//			// Group sessions by the week of the year
//			var sessionsByWeek = sessions.GroupBy(s =>
//				CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday));

//			if (planType == "TrainerRental")
//			{
//				foreach (var week in sessionsByWeek)
//				{
//					if (week.Count() != 5)
//						return (false, "Trainer rental plan requires exactly 5 sessions per week.");
//				}
//			}
//			else if (planType == "Boxing")
//			{
//				var validDays1 = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
//				var validDays2 = new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday };

//				foreach (var week in sessionsByWeek)
//				{
//					var daysInWeek = week.Select(s => s.Date.DayOfWeek).ToList();
//					if (week.Count() != 3 ||
//						(!daysInWeek.SequenceEqual(validDays1) && !daysInWeek.SequenceEqual(validDays2)))
//					{
//						return (false, "Boxing plan sessions must be exactly 3 per week on Mon/Wed/Fri or Tue/Thu/Sat.");
//					}
//				}
//			}

//			return (true, string.Empty);
//		}

//		private async Task<bool> CheckTrainerAvailability(string trainerId, List<Slot> sessions)
//		{
//			foreach (var session in sessions)
//			{
//				var trainerSessions = await _firebaseClient
//					.Child("Sessions")
//					.OrderBy("TrainerId")
//					.EqualTo(trainerId)
//					.OnceAsync<Session>();

//				var sessionCountForDate = trainerSessions
//					.Where(s => s.Object.Date.Date == session.Date.Date)
//					.Count();

//				// Assuming a max of 8 sessions per day for a trainer
//				if (sessionCountForDate >= 8)
//					return false;
//			}
//			return true;
//		}

//		// GET: api/Schedule/Trainer/{trainerId}/Availability/{date}
//		[HttpGet("Trainer/{trainerId}/Availability/{date}")]
//		public async Task<IActionResult> GetTrainerAvailability(string trainerId, DateTime date)
//		{
//			var sessions = await _firebaseClient
//				.Child("Sessions")
//				.OrderBy("TrainerId")
//				.EqualTo(trainerId)
//				.OnceAsync<Session>();

//			int maxSessionsPerDay = 8;
//			int sessionCountForDate = sessions
//				.Where(s => s.Object.Date.Date == date.Date)
//				.Count();

//			int remainingSlots = maxSessionsPerDay - sessionCountForDate;

//			return remainingSlots <= 0
//				? Ok("Trainer is fully booked for this date.")
//				: Ok($"Trainer has {remainingSlots} slots available on {date:yyyy-MM-dd}.");
//		}
//	}
//}


using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ScheduleController : ControllerBase
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public ScheduleController(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		// GET: api/Schedule/User/{userId}
		[HttpGet("User/{userId}")]
		public async Task<IActionResult> GetUserSchedules(string userId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var schedules = await _firebaseClient
				.Child("Schedules")
				.OrderBy("UserId")
				.EqualTo(userId)
				.OnceAsync<Schedule>();

			if (!schedules.Any())
				return NotFound("No schedules found for this user.");

			foreach (var schedule in schedules)
			{
				schedule.Object.ScheduleId = schedule.Key;
			}

			return Ok(schedules.Select(s => s.Object));
		}

		// POST: api/Schedule
		[HttpPost]
		public async Task<IActionResult> CreateSchedule(string planType, Schedule schedule, List<Slot> sessions)
		{
			// Validate sessions based on the plan type
			var validation = ValidateSessions(planType, sessions);
			if (!validation.IsValid)
				return BadRequest(validation.ErrorMessage);

			// Check if Trainer is fully booked on the days of the new sessions
			var trainerAvailability = await CheckTrainerAvailability(schedule.TrainerId, sessions);
			if (!trainerAvailability)
				return BadRequest("Trainer is fully booked for one or more session dates.");

			// Generate IDs and save schedule to Firebase
			schedule.ScheduleId = Guid.NewGuid().ToString();
			schedule.SessionCount = sessions.Count;
			schedule.FirstSession = sessions.Min(s => s.Date);
			schedule.LastSession = sessions.Max(s => s.Date);

			await _firebaseClient
				.Child("Schedules")
				.Child(schedule.ScheduleId)
				.PutAsync(schedule);

			foreach (var session in sessions)
			{
				session.SlotId = Guid.NewGuid().ToString();
				session.ScheduleId = schedule.ScheduleId;

				await _firebaseClient
					.Child("Sessions")
					.Child(session.SlotId)
					.PutAsync(session);
			}

			return Ok("Schedule created successfully.");
		}

		private (bool IsValid, string ErrorMessage) ValidateSessions(string planType, List<Slot> sessions)
		{
			// Group sessions by both year and week number
			var sessionsByWeek = sessions
				.GroupBy(s => new
				{
					Year = s.Date.Year,
					Week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.Date.ToDateTime(s.StartTime), CalendarWeekRule.FirstDay, DayOfWeek.Monday)
				});

			var firstSessionYear = sessionsByWeek.FirstOrDefault().Select(s => s.Date.Year).OrderBy(d => d).First();
			var lastSessionYear = sessionsByWeek.LastOrDefault().Select(s => s.Date.Year).OrderBy(d => d).Last();

			if (firstSessionYear < lastSessionYear)
			{
				//process here
			}

			if (planType == "TrainerRental")
			{
				// TrainerRental plan should have exactly 5 sessions per full week
				foreach (var weekGroup in sessionsByWeek)
				{
					if (weekGroup.Count() != 5)
						return (false, "Trainer rental plan requires exactly 5 sessions per week.");
				}
			}
			else if (planType == "Boxing")
			{
				// Boxing plan should have exactly 3 sessions on Mon/Wed/Fri or Tue/Thu/Sat, and every week must follow the same pattern
				var firstWeek = sessionsByWeek.FirstOrDefault();

				// Determine the valid days pattern from the first week
				var daysInFirstWeek = firstWeek.Select(s => s.Date.DayOfWeek).OrderBy(d => d).ToList();
				var validDaysSet1 = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
				var validDaysSet2 = new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday };

				if (daysInFirstWeek.Count != 3 ||
					!(daysInFirstWeek.SequenceEqual(validDaysSet1) || daysInFirstWeek.SequenceEqual(validDaysSet2)))
				{
					return (false, "Boxing plan must have exactly 3 sessions per week on either Mon/Wed/Fri or Tue/Thu/Sat, consistently.");
				}

				// Use this pattern to validate each subsequent week
				foreach (var weekGroup in sessionsByWeek)
				{
					var daysInWeek = weekGroup.Select(s => s.Date.DayOfWeek).OrderBy(d => d).ToList();
					if (!daysInWeek.SequenceEqual(daysInFirstWeek))
					{
						return (false, "All weeks must follow the same days pattern established in the first week: either Mon/Wed/Fri or Tue/Thu/Sat.");
					}
				}
			}

			return (true, string.Empty);
		}


		private async Task<bool> CheckTrainerAvailability(string trainerId, List<Slot> slots)
		{
			foreach (var slot in slots)
			{
				var trainerSessions = await _firebaseClient
					.Child("Sessions")
					.OrderBy("TrainerId")
					.EqualTo(trainerId)
					.OnceAsync<Slot>();

				var sessionCountForDate = trainerSessions
					.Where(s => s.Object.Date == slot.Date)
					.Count();

				// Assuming a max of 8 sessions per day for a trainer
				if (sessionCountForDate >= 8)
					return false;
			}
			return true;
		}

		// GET: api/Schedule/Trainer/{trainerId}/Availability/{date}
		[HttpGet("Trainer/{trainerId}/Availability/{date}")]
		public async Task<IActionResult> GetTrainerAvailability(string trainerId, DateTime date)
		{
			var sessions = await _firebaseClient
				.Child("Sessions")
				.OrderBy("TrainerId")
				.EqualTo(trainerId)
				.OnceAsync<Slot>();

			int maxSessionsPerDay = 8;
			int sessionCountForDate = sessions
				.Where(s => s.Object.Date == DateOnly.FromDateTime(date))
				.Count();

			int remainingSlots = maxSessionsPerDay - sessionCountForDate;

			return remainingSlots <= 0
				? Ok("Trainer is fully booked for this date.")
				: Ok($"Trainer has {remainingSlots} slots available on {date:yyyy-MM-dd}.");
		}
	}
}
