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

			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var boxReg = _firebaseClient.Child("BoxingRegistrations").OrderBy("isActive").EqualTo(true).OnceAsync<BoxingRegistration>();

			var gymReg = _firebaseClient.Child("GymRegistrations").OrderBy("userId").EqualTo(request.UserId).OnceAsync<GymRegistration>();

			var rentalReg = _firebaseClient.Child("TrainerRentalRegistrations").OrderBy("isActive").EqualTo(true).OnceAsync<TrainerRentalRegistration>();

			await Task.WhenAll(boxReg, gymReg);

			var gymMembership = gymReg.Result.FirstOrDefault(exReg =>
				exReg.Object.IsActive &&
				(exReg.Object.EndDate >= DateTime.Now || exReg.Object.SessionLeft > 0));

			var boxingMembership = boxReg.Result.FirstOrDefault(reg =>
				reg.Object.UserIds != null &&
				reg.Object.UserIds.Split(',').Any(userId => userId.Equals(request.UserId)) &&
				(reg.Object.SessionLeft > 0 && reg.Object.EndDate >= DateTime.Now));

			var rentalMembership = rentalReg.Result.FirstOrDefault(reg =>
					reg.Object.UserIds != null &&
					reg.Object.UserIds.Split(',').Any(userId => userId.Equals(request.UserId)) &&
					(reg.Object.EndDate >= DateTime.Now || reg.Object.SessionLeft > 0));

			var hasGymMembership = gymMembership != null;
			var hasBoxingMembership = boxingMembership != null;
			var hasTrainerRentalMembership = rentalMembership != null;

			if (!hasGymMembership && !hasBoxingMembership)
			{
				return BadRequest("User do not have any memberships");
			}

			// Create and store the check-in
			var newCheckIn = new
			{
				userId = request.UserId,
				time = request.Time
			};

			if (hasGymMembership && !hasTrainerRentalMembership && !hasBoxingMembership)
			{
				var checkInTask = _firebaseClient.Child("CheckIns").PostAsync(newCheckIn);
				var gymTask = _firebaseClient.Child("GymRegistrations").Child(gymMembership.Object.RegistrationId).PatchAsync(new
				{
					sessionLeft = gymMembership.Object.SessionLeft - 1
				});

				await Task.WhenAll(checkInTask, gymTask);
				return Ok("Check-in recorded successfully.");
			}

			// Cache current time details
			var currentDate = DateOnly.FromDateTime(DateTime.Now);
			var currentTime = TimeOnly.FromDateTime(DateTime.Now);

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
				return Ok("No schedules found for the user.");

			// Pre-fetch all slots related to the schedules
			var scheduleKeys = schedules.Select(s => s.Key).ToHashSet();
			var slots = (await _firebaseClient
				.Child("Slots")
				.OnceAsync<Slot>())
				.Where(slot => scheduleKeys.Contains(slot.Object.ScheduleId) && slot.Object.Date == currentDate)
				.ToList();

			if (!slots.Any())
			{
				if (hasBoxingMembership && !hasTrainerRentalMembership && !hasGymMembership)
				{
					return Ok("No slots found for today.");
				}
				else
				{
					var checkInTask = _firebaseClient.Child("CheckIns").PostAsync(newCheckIn);
					var gymTask = _firebaseClient.Child("GymRegistrations").Child(gymMembership.Object.RegistrationId).PatchAsync(new
					{
						sessionLeft = gymMembership.Object.SessionLeft - 1
					});

					await Task.WhenAll(checkInTask, gymTask);
					return Ok("Check-in recorded successfully.");
				}
			}
			else
			{
				// Process slots and update attendance
				foreach (var slot in slots)
				{
					var timeRange = _timeSlotService.GetTimeSlot(slot.Object.TimeSlotId).Split('-');
					if (timeRange.Length == 2 &&
						//customer comes 30 minutes early
						TimeOnly.ParseExact(timeRange[0], "H:mm").AddMinutes(-30) <= currentTime &&
						TimeOnly.ParseExact(timeRange[1], "H:mm") >= currentTime)
					{
						var slotTask = _firebaseClient.Child("Slots").Child(slot.Key).PatchAsync(new
						{
							attended = true
						});

						var boxingTask = _firebaseClient.Child("BoxingRegistrations").Child(boxingMembership.Object.RegistrationId).PatchAsync(new
						{
							sessionLeft = boxingMembership.Object.SessionLeft - 1
						});

						var gymTask = _firebaseClient.Child("GymRegistrations").Child(gymMembership.Object.RegistrationId).PatchAsync(new
						{
							sessionLeft = gymMembership.Object.SessionLeft - 1
						});

						var rentalTask = _firebaseClient.Child("TrainerRentalRegistrations").Child(rentalMembership.Object.RegistrationId).PatchAsync(new
						{
							sessionLeft = rentalMembership.Object.SessionLeft - 1
						});

						if (hasBoxingMembership && !hasGymMembership)
						{
							await Task.WhenAll(slotTask, boxingTask);
						}
						else if (hasBoxingMembership && hasGymMembership && !hasTrainerRentalMembership)
						{
							await Task.WhenAll(slotTask, boxingTask, gymTask);
						}
						else if (!hasBoxingMembership && hasGymMembership && hasTrainerRentalMembership)
						{
							await Task.WhenAll(slotTask, gymTask, rentalTask);
						}
						else if (hasBoxingMembership && hasGymMembership && hasTrainerRentalMembership)
						{
							if (slot.Object.ScheduleId == boxingMembership.Object.ScheduleId)
								await Task.WhenAll(slotTask, boxingTask, gymTask);
							else
								await Task.WhenAll(slotTask, gymTask, rentalTask);
						}
						return Ok($"Check-in recorded successfully. Slot: {_timeSlotService.GetTimeSlot(slot.Object.TimeSlotId)}");
					}
				}

				//if (currentTime > slots.Max(x => TimeOnly.ParseExact(_timeSlotService.GetTimeSlot(x.Object.TimeSlotId).Split('-')[0], "H:mm")))
				//{
				//	return Ok("Check-in recorded successfully.");
				//}

				//too late or too early
				if (hasBoxingMembership && !hasTrainerRentalMembership && !hasGymMembership)
					return Conflict("Check-in failed. No slot available now");
				else
				{
					var checkInTask = _firebaseClient.Child("CheckIns").PostAsync(newCheckIn);
					var gymTask = _firebaseClient.Child("GymRegistrations").Child(gymMembership.Object.RegistrationId).PatchAsync(new
					{
						sessionLeft = gymMembership.Object.SessionLeft - 1
					});

					await Task.WhenAll(checkInTask, gymTask);
					return Ok("Check-in recorded successfully.");
				}
			}
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
