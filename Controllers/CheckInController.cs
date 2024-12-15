using Microsoft.AspNetCore.Mvc;
using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;

namespace Alpha_API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CheckInController : ControllerBase
	{
		private const int CheckInLateIflaterThanMinutes = 15;
		private const int StaffIsAbsenceIfWorkLessThanHours = 4;
		private const int TrainerIsAbsenceIfWorkLessThanMinutes = 30;
		private readonly TimeSlotService _timeSlotService;
		private readonly TrainerService _trainerService;
		private readonly RoleService _roleService;
		private readonly ShiftService _shiftService;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private FirebaseClient _firebaseClient;

		public CheckInController(FirebaseClientProvider firebaseClientProvider, TimeSlotService timeSlotService, RoleService roleService
			, ShiftService shiftService, TrainerService trainerService)
		{
			_firebaseClientProvider = firebaseClientProvider;
			_timeSlotService = timeSlotService;
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			_roleService = roleService;
			_shiftService = shiftService;
			_trainerService = trainerService;
		}

		[HttpPost]
		public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
		{
			if (request?.UserId == null || request.Time == null)
			{
				return BadRequest("Invalid request data.");
			}

			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var role = await _roleService.GetRoleOfUser(request.UserId);

			// Create and store the check-in
			var newCheckIn = new
			{
				userId = request.UserId,
				time = request.Time
			};

			if (role.Equals("customer"))
			{

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
					return NotFound("User do not have any memberships");
				}

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
					return NotFound("No schedules found for the user.");

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
						return Conflict("No slots found for today.");
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

			else if (role.Equals("staff"))
			{
				try
				{
					var currentShift = await _shiftService.ShiftAtTimeAsync(request.UserId, (DateTime)request.Time);

					if (currentShift != null)
					{
						// Calculate if the staff is late
						var checkTime = TimeOnly.FromDateTime((DateTime)request.Time);
						var shiftStartTime = TimeOnly.FromDateTime(currentShift.StartTime);
						var shiftEndTime = TimeOnly.FromDateTime(currentShift.EndTime);

						// Determine lateness
						var isLate = (checkTime - shiftStartTime) > TimeSpan.FromMinutes(CheckInLateIflaterThanMinutes); //staff has x minutes to checkin after start time of shift
						var isPresent = (shiftEndTime - checkTime) >= TimeSpan.FromHours(StaffIsAbsenceIfWorkLessThanHours);

						var attendance = new AttendanceRecord()
						{
							TrainerId = "",
							StaffId = request.UserId,
							Time = (DateTime)request.Time,
							IsLate = isPresent ? isLate : false,
							IsPresent = isPresent,
						};
						var options = new JsonSerializerOptions
						{
							PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
							DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
						};

						var jsonString = JsonSerializer.Serialize(attendance, options);

						await _firebaseClient.Child("AttendanceRecords").PostAsync(jsonString);
						return Ok("Staff attendance recorded successfully.");
					}
					else
					{
						return NotFound("No shift now");
					}
				}
				catch (InvalidOperationException ex)
				{
					return Conflict(new { message = ex.Message });
				}

			}

			else if (role.Equals("pt"))
			{
				try
				{
					// Check if any slot is available now
					var currentSlot = await _trainerService.SlotAtTimeAsync(request.UserId, (DateTime)request.Time);

					if (currentSlot != null)
					{
						// Calculate if the staff is late
						var checkTime = TimeOnly.FromDateTime((DateTime)request.Time);
						var timeSlot = _timeSlotService.GetTimeSlot(currentSlot.SlotId).Split("-");
						var slotStartTime = TimeOnly.ParseExact(timeSlot[0], "H:mm");
						var slotEndTime = TimeOnly.ParseExact(timeSlot[1], "H:mm");

						// Determine lateness
						var isLate = (checkTime - slotStartTime) > TimeSpan.FromMinutes(CheckInLateIflaterThanMinutes);
						var isPresent = (slotEndTime - checkTime) >= TimeSpan.FromMinutes(TrainerIsAbsenceIfWorkLessThanMinutes);

						var attendance = new AttendanceRecord()
						{
							TrainerId = request.UserId,
							StaffId = "",
							Time = (DateTime)request.Time,
							IsLate = isPresent ? isLate : false,
							IsPresent = isPresent,
						};
						var options = new JsonSerializerOptions
						{
							PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
							DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
						};

						var jsonString = JsonSerializer.Serialize(attendance, options);

						await _firebaseClient.Child("AttendanceRecords").PostAsync(jsonString);

						return Ok($"Trainer attendance recorded successfully.");
					}
					else
					{
						return Conflict("No slot now");
					}
				}
				catch (InvalidOperationException ex)
				{
					return Conflict(new { message = ex.Message });
				}
				catch (Exception ex)
				{
					// Log the exception (if using a logger)
					return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
				}
			}

			else
			{
				return NotFound("Role not found for this user.");
			}
		}

        [Authorize(Roles = "admin,staff,customer")]
        [HttpGet("checkInDates/{userId}")]
        public async Task<IActionResult> GetCheckInDatesByUserId(string userId)
        {
            // Lấy dữ liệu check-in từ Firebase
            var checkIns = (await _firebaseClient
                .Child("CheckIns")
                .OnceAsync<CheckIn>())
                .Where(c => c.Object.UserId == userId)
                .Select(c => c.Object)
                .ToList();

            if (!checkIns.Any())
            {
                // Trả về JSON với CheckInDates là một mảng trống
                return Ok(new CheckInDatesResponse { CheckInDates = new List<CheckInDateInfo>() });
            }

            // Nhóm check-in theo ngày (chỉ lấy ngày mà không lấy giờ)
            var checkInDates = checkIns
                .Where(c => c.Time.HasValue)
                .GroupBy(c => c.Time.Value.Date)
                .Select(g => new CheckInDateInfo
                {
                    Date = g.Key,
                    // Lấy thời gian đầu tiên của ngày và chỉ lấy giờ và phút
                    LastCheckInTime = g.OrderBy(c => c.Time.Value).Last().Time.Value.ToString("HH:mm")
                })
                .OrderBy(info => info.Date) // Sắp xếp theo ngày
                .ToList();

            // Trả về danh sách các ngày check-in và thời gian check-in đầu tiên
            return Ok(new CheckInDatesResponse { CheckInDates = checkInDates });
        }




        [HttpGet("hasCheckIn/{userId}/{date}")]
		public async Task<IActionResult> HasCheckInOnDate(string userId, string date)
		{
			// Chuyển chuỗi ngày nhận được từ query (format yyyy-MM-dd) thành DateTime
			if (!DateTime.TryParse(date, out var parsedDate))
			{
				return BadRequest("Invalid date format. Please use yyyy-MM-dd.");
			}

			// Lấy dữ liệu check-in từ Firebase
			var checkIns = (await _firebaseClient
				.Child("CheckIns")
				.OnceAsync<CheckIn>())
				.Where(c => c.Object.UserId == userId)
				.Select(c => c.Object)
				.ToList();

			// Kiểm tra nếu có check-in cho ngày yêu cầu
			var hasCheckIn = checkIns.Any(c => c.Time?.Date == parsedDate.Date);

			// Trả về true nếu có check-in trong ngày đó, ngược lại false
			return Ok(new { HasCheckIn = hasCheckIn });
		}
	}

	// Models
	public class CheckIn
	{
		public string UserId { get; set; }
		public DateTime? Time { get; set; }
	}
    public class CheckInDatesResponse
    {
        public List<CheckInDateInfo> CheckInDates { get; set; }
    }

    public class CheckInDateInfo
    {
        public DateTime Date { get; set; }
        public string LastCheckInTime { get; set; } // Định dạng "HH:mm"
    }

    public class CheckInRequest
	{
		public string UserId { get; set; }
		public DateTime? Time { get; set; }
	}
}
