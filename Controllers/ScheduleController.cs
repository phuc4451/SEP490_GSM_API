using Alpha_API.Models;
using Alpha_API.Services;
using Alpha_API.ViewModel;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Alpha_API.Utils;
using System.Text;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Office2016.Excel;
using System.Security.Claims;
using ExcelDataReader.Log;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ScheduleController : ControllerBase
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly EmailService _emailService;
		private readonly TimeSlotService _timeSlotService;


		public ScheduleController(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider, EmailService emailService, TimeSlotService timeSlotService)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			_emailService = emailService;
			_timeSlotService = timeSlotService;
		}

		// GET: api/Schedule/Customer/{userId}
		[HttpGet("Customer/{userId}")]
		public async Task<ActionResult<IEnumerable<Schedule>>> GetCustomerSchedules(string userId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var schedules = await _firebaseClient
			.Child("Schedules")
			.OnceAsync<Schedule>();

			var userSchedules = schedules.Where(sche =>
				sche.Object.UserIds != null &&
				sche.Object.UserIds.Split(',').Contains(userId)
				);

			if (!userSchedules.Any())
				return NotFound("No schedules found for this customer.");

			foreach (var schedule in userSchedules)
			{
				schedule.Object.ScheduleId = schedule.Key;
			}

			return userSchedules.Select(s => s.Object).ToList();
		}

        [HttpGet("Slot/Customer/{userId}")]
        public async Task<ActionResult<IEnumerable<Object>>> GetCustomerSchedulesByDate(string userId)
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

            // Lấy tất cả Schedules và Slots trong một lần truy vấn
            var schedulesTask = _firebaseClient.Child("Schedules").OnceAsync<Schedule>();
            var slotsTask = _firebaseClient.Child("Slots").OnceAsync<Slot>();
            var trainersTask = _firebaseClient.Child("Trainers").OnceAsync<Trainer>();

            await Task.WhenAll(schedulesTask, slotsTask, trainersTask);

            var schedules = schedulesTask.Result;
            var slots = slotsTask.Result;
            var trainers = trainersTask.Result;

            // Lọc danh sách schedules theo userId
            var userSchedules = schedules
                .Where(sche => sche.Object.UserIds != null && sche.Object.UserIds.Split(',').Contains(userId))
                .ToList();

            if (!userSchedules.Any())
                return NotFound("No schedules found for this customer.");

            // Xây dựng kết quả
            var result = slots
                .Join(userSchedules, slot => slot.Object.ScheduleId, schedule => schedule.Key, (slot, schedule) => new
                {
                    Slot = slot,
                    Schedule = schedule
                })
                .Select(item =>
                {
                    var trainer = trainers.FirstOrDefault(t => t.Key == item.Schedule.Object.TrainerId);
                    return new
                    {
                        trainerName = trainer?.Object.Name,
                        timeSlot = _timeSlotService.GetTimeSlot(item.Slot.Object.TimeSlotId),
                        date = item.Slot.Object.Date.ToString("yyyy-MM-dd")
                    };
                })
                .ToList();

            return result;
        }


        [HttpGet("Slot/Trainer/{userId}")]
        public async Task<ActionResult<IEnumerable<Object>>> GetTrainerCustomerSchedulesByDate(string userId)
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

            // Fetch the trainer directly
            var trainer = (await _firebaseClient
                .Child("Trainers")
                .OnceAsync<Trainer>())
                .FirstOrDefault(t => t.Object.UserId == userId);

            if (trainer == null)
                return NotFound("Trainer not found for the given userId.");

            var trainerId = trainer.Key;

            // Fetch only schedules related to the trainer
            var schedules = await _firebaseClient
                .Child("Schedules")
                .OrderBy("trainerId")
                .EqualTo(trainerId)
                .OnceAsync<Schedule>();

            if (!schedules.Any())
                return NotFound("No schedules found for this trainer.");

            // Build a dictionary for schedules
            var scheduleDict = schedules.ToDictionary(s => s.Key, s => s.Object);

            // Extract schedule IDs
            var scheduleIds = scheduleDict.Keys.ToList();

            // Fetch all slots (consider limiting by date range if possible)
            var allSlots = await _firebaseClient.Child("Slots").OnceAsync<Slot>();
            var slots = allSlots.Where(s => scheduleIds.Contains(s.Object.ScheduleId)).ToList();

            if (!slots.Any())
                return NotFound("No slots found for this trainer's schedules.");

            // Collect user IDs from schedules
            var userIds = schedules
                .SelectMany(s => s.Object.UserIds?.Split(',') ?? Array.Empty<string>())
                .Distinct()
                .ToList();

            // Fetch only the necessary users
            var userTasks = userIds.Select(id =>
                _firebaseClient.Child("users").Child(id).OnceSingleAsync<User>()
            );

            var users = await Task.WhenAll(userTasks);
            var userDict = userIds.Zip(users, (id, user) => new { id, user })
                                  .ToDictionary(x => x.id, x => x.user);

            // Build a dictionary for time slots
            var timeSlotIds = slots.Select(s => s.Object.TimeSlotId).Distinct();
            var timeSlotDict = timeSlotIds.ToDictionary(
                id => id,
                id => _timeSlotService.GetTimeSlot(id)
            );

            // Group schedules by date and time slot
            var groupedSchedules = new Dictionary<string, Dictionary<string, List<string>>>();

            foreach (var slot in slots)
            {
                var scheduleId = slot.Object.ScheduleId;

                if (scheduleDict.TryGetValue(scheduleId, out var schedule))
                {
                    var timeSlot = timeSlotDict[slot.Object.TimeSlotId];
                    var date = slot.Object.Date.ToString("yyyy-MM-dd");

                    if (!groupedSchedules.ContainsKey(date))
                        groupedSchedules[date] = new Dictionary<string, List<string>>();

                    if (!groupedSchedules[date].ContainsKey(timeSlot))
                        groupedSchedules[date][timeSlot] = new List<string>();

                    // Add customer names to the group
                    var scheduleUserIds = schedule.UserIds?.Split(',') ?? Array.Empty<string>();
                    foreach (var customerId in scheduleUserIds)
                    {
                        if (userDict.TryGetValue(customerId, out var customer))
                            groupedSchedules[date][timeSlot].Add(customer.Name);
                    }
                }
            }

            // Prepare the result
            var result = groupedSchedules.SelectMany(dateGroup =>
                dateGroup.Value.Select(timeSlotGroup => new
                {
                    date = dateGroup.Key,
                    timeSlot = timeSlotGroup.Key,
                    customers = timeSlotGroup.Value
                })).ToList();

            if (!result.Any())
                return NotFound("No schedules found for this trainer.");

            return result;
        }





        // GET: api/Schedule/Trainer/{userId}
        [HttpGet("Trainer/{userId}")]
		public async Task<ActionResult<IEnumerable<Schedule>>> GetTrainerSchedules(string userId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var schedules = await _firebaseClient
			.Child("Schedules")
			.OrderBy("trainerId")
			.EqualTo(userId)
			.OnceAsync<Schedule>();

			var userSchedules = schedules.Where(sche =>
				sche.Object.TrainerId != null &&
				sche.Object.TrainerId.Equals(userId)
				);

			if (!userSchedules.Any())
				return NotFound("No schedules found for this trainer.");

			foreach (var schedule in userSchedules)
			{
				schedule.Object.ScheduleId = schedule.Key;
			}

			return userSchedules.Select(s => s.Object).ToList();
		}

		// GET: api/Schedule/MySchedules
		[HttpGet("MySchedules")]
		public async Task<ActionResult<IEnumerable<Schedule>>> GetMySchedules()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Retrieve the uid claim
			var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

			if (userIdClaim == null)
			{
				return Unauthorized("User not authenticated.");
			}

			var userId = userIdClaim.Value; // This is the Firebase UID

			var schedules = await _firebaseClient
			.Child("Schedules")
			.OnceAsync<Schedule>();
			//chua dc
			var userSchedules = schedules.Where(sche =>
				sche.Object.UserIds != null &&
				sche.Object.UserIds.Equals(userId)
				);

			if (!userSchedules.Any())
				return NotFound("No schedules found for this trainer.");

			foreach (var schedule in userSchedules)
			{
				schedule.Object.ScheduleId = schedule.Key;
			}

			return userSchedules.Select(s => s.Object).ToList();
		}

        [HttpGet("TimeSlots")]
        public async Task<ActionResult<IEnumerable<TimeSlot>>> GetAllTimeSlots()
        {
            // Lấy client Firebase
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

            // Lấy danh sách TimeSlots từ node "TimeSlots"
            var timeSlots = await _firebaseClient
                .Child("TimeSlots")
                .OnceAsync<TimeSlot>();

            // Kiểm tra nếu không có TimeSlot nào
            if (!timeSlots.Any())
            {
                return NotFound("No TimeSlots found.");
            }

            // Map dữ liệu từ Firebase thành danh sách TimeSlot
            var result = timeSlots.Select(ts => new TimeSlot
            {
                TimeSlotId = ts.Key,
                Time = ts.Object.Time
            }).ToList();

            return result;
        }

        //// POST: api/Schedule
        //[HttpPost]
        //public async Task<IActionResult> CreateSchedule([FromBody] RegisterScheduleRequest request)
        //{
        //	var options = new JsonSerializerOptions
        //	{
        //		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        //	};
        //	string planType = "";
        //	string trainerId = "";
        //	int numberOfUsers = request.Emails.Count;

        //	if (numberOfUsers == 0)
        //	{
        //		return BadRequest("No emails in request");
        //	}

        //	if (string.IsNullOrEmpty(request.TrainerRentalPlanId) && string.IsNullOrEmpty(request.BoxingMembershipPlanId))
        //	{
        //		return BadRequest("Memberships or plans are null");
        //	}

        //	List<string> userIds = new List<string>();
        //	StringBuilder userIdsBuilder = new StringBuilder();


        //	foreach (var email in request.Emails)
        //	{
        //		var userId = await _emailService.GetUserIdByEmail(email);
        //		userIds.Add(userId);
        //	}

        //	foreach (var userId in userIds)
        //		userIdsBuilder.Append(userId).Append(",");
        //	if (userIdsBuilder.Length > 0)
        //	{
        //		userIdsBuilder.Length--; // This removes the last comma
        //	}

        //	string userIdsString = userIdsBuilder.ToString();

        //	if (!string.IsNullOrEmpty(request.TrainerRentalPlanId))
        //	{
        //		planType = "TrainerRental";

        //		var plan = await _firebaseClient
        //			.Child("TrainerRentalPlans")
        //			.Child(request.TrainerRentalPlanId)
        //			.OnceSingleAsync<TrainerRentalPlan>();

        //		trainerId = plan.TrainerId;

        //		var option = await _firebaseClient
        //			.Child("RentalOptions")
        //			.Child(plan.RentalOptionId)
        //			.OnceSingleAsync<RentalOption>();
        //		option.RentalOptionId = plan.RentalOptionId;

        //		if (option.MemberCount != numberOfUsers)
        //		{
        //			return BadRequest($"This plan is for {option.MemberCount} persons");
        //		}

        //		if (!request.Duration.HasValue || request.Duration == 0)
        //		{
        //			return BadRequest("Please type months or sessions");
        //		}

        //		//rental option for months
        //		if (option.SessionCountMax == 0 && option.SessionCountMin == 0)
        //		{
        //			// Calculate slots
        //			int duration = request.Duration.Value;
        //			var startDate = DateOnly.FromDateTime(DateTime.Now);
        //			var endDate = startDate.AddDays(1).AddMonths(duration);
        //			var slotDates = GenerateSlotDates(startDate, endDate, request.IsMonWedFri, false, 0);

        //			// Generate schedule and slots
        //			var scheduleId = Guid.NewGuid().ToString();
        //			var slots = new List<Slot>();
        //			int slotCount = 0;

        //			// Split the string at the hyphen to separate start and end times
        //			var times = request.SelectedTimeSlot.Split('-');

        //			foreach (var date in slotDates)
        //			{
        //				var slot = new Slot
        //				{
        //					ScheduleId = scheduleId,
        //					Date = date,
        //					StartTime = TimeOnly.ParseExact(times[0], "H:mm"),
        //					EndTime = TimeOnly.ParseExact(times[1], "H:mm"),
        //					Attended = false
        //				};
        //				slots.Add(slot);
        //				slotCount++;
        //			}

        //			// Check if Trainer is fully booked on the days of the new slots
        //			var trainerAvailability = await CheckTrainerAvailability(trainerId, slots);
        //			if (!trainerAvailability)
        //				return BadRequest("Trainer is fully booked for one or more slot dates.");

        //			var schedule = new
        //			{
        //				ScheduleId = scheduleId,
        //				UserIds = userIdsString, // user IDs as comma-separated values
        //				TrainerId = plan.TrainerId,
        //				FirstSlot = slotDates.Min(d => d),
        //				LastSlot = slotDates.Max(d => d),
        //				SlotCount = slotCount
        //			};

        //			// Save schedule and slots to Firebase
        //			var jsonString = JsonSerializer.Serialize(schedule, options);
        //			await _firebaseClient.Child("Schedules").Child(scheduleId).PutAsync(jsonString);
        //			foreach (var slot in slots)
        //			{
        //				jsonString = JsonSerializer.Serialize(slot, options);
        //				await _firebaseClient.Child("Slots").PostAsync(jsonString);
        //			}
        //			return Ok("Schedule created successfully.");
        //		}
        //		else
        //		{
        //			//rental option for sessions
        //			if (request.Duration > option.SessionCountMax || request.Duration < option.SessionCountMin)
        //			{
        //				return BadRequest($"The number of sessions registered are invalid with this rental plan");
        //			}

        //			// Calculate slots
        //			int duration = request.Duration.Value;
        //			var startDate = DateOnly.FromDateTime(DateTime.Now);
        //			var endDate = startDate.AddDays(duration);
        //			var slotDates = GenerateSlotDates(startDate, endDate, request.IsMonWedFri, false, duration);

        //			// Generate schedule and slots
        //			var scheduleId = Guid.NewGuid().ToString();
        //			var slots = new List<Slot>();
        //			int slotCount = 0;

        //			// Split the string at the hyphen to separate start and end times
        //			var times = request.SelectedTimeSlot.Split('-');

        //			foreach (var date in slotDates)
        //			{
        //				var slot = new Slot
        //				{
        //					ScheduleId = scheduleId,
        //					Date = date,
        //					StartTime = TimeOnly.ParseExact(times[0], "H:mm"),
        //					EndTime = TimeOnly.ParseExact(times[1], "H:mm"),
        //					Attended = false
        //				};
        //				slots.Add(slot);
        //				slotCount++;
        //			}

        //			// Check if Trainer is fully booked on the days of the new slots
        //			var trainerAvailability = await CheckTrainerAvailability(trainerId, slots);
        //			if (!trainerAvailability)
        //				return BadRequest("Trainer is fully booked for one or more slot dates.");

        //			var schedule = new
        //			{
        //				ScheduleId = scheduleId,
        //				UserIds = userIdsString, // user IDs as comma-separated values
        //				TrainerId = plan.TrainerId,
        //				FirstSlot = slotDates.Min(d => d),
        //				LastSlot = slotDates.Max(d => d),
        //				SlotCount = slotCount
        //			};

        //			// Save schedule and slots to Firebase
        //			var jsonString = JsonSerializer.Serialize(schedule, options);
        //			await _firebaseClient.Child("Schedules").Child(scheduleId).PutAsync(jsonString);
        //			foreach (var slot in slots)
        //			{
        //				jsonString = JsonSerializer.Serialize(slot, options);
        //				await _firebaseClient.Child("Slots").PostAsync(jsonString);
        //			}
        //			return Ok("Schedule created successfully.");
        //		}
        //	}
        //	else if (!string.IsNullOrEmpty(request.BoxingMembershipPlanId))
        //	{
        //		planType = "Boxing";

        //		var plan = await _firebaseClient
        //			.Child("BoxingMembershipPlans")
        //			.Child(request.BoxingMembershipPlanId)
        //			.OnceSingleAsync<BoxingMembershipPlan>();

        //		trainerId = plan.BoxingTrainerId;

        //		var option = await _firebaseClient
        //			.Child("BoxingOptions")
        //			.Child(plan.BoxingOptionId)
        //			.OnceSingleAsync<BoxingOption>();

        //		if (option.MemberCount != numberOfUsers)
        //		{
        //			return BadRequest($"This plan is for {option.MemberCount} persons");
        //		}

        //		if (!request.Duration.HasValue || request.Duration == 0)
        //		{
        //			return BadRequest($"Please fill in number of months to register");
        //		}

        //		// Calculate slots
        //		int duration = option.Sessions;
        //		var startDate = DateOnly.FromDateTime(DateTime.Now);
        //		var endDate = startDate.AddDays(1).AddMonths(1);
        //		var slotDates = GenerateSlotDates(startDate, endDate, request.IsMonWedFri, true, duration);

        //		// Generate schedule and slots
        //		var scheduleId = Guid.NewGuid().ToString();
        //		var slots = new List<Slot>();
        //		int slotCount = 0;

        //		// Split the string at the hyphen to separate start and end times
        //		var times = request.SelectedTimeSlot.Split('-');

        //		foreach (var date in slotDates)
        //		{
        //			var slot = new Slot
        //			{
        //				ScheduleId = scheduleId,
        //				Date = date,
        //				StartTime = TimeOnly.ParseExact(times[0], "H:mm"),
        //				EndTime = TimeOnly.ParseExact(times[1], "H:mm"),
        //				Attended = false
        //			};
        //			slots.Add(slot);
        //			slotCount++;
        //		}

        //		// Check if Trainer is fully booked on the days of the new slots
        //		var trainerAvailability = await CheckTrainerAvailability(trainerId, slots);
        //		if (!trainerAvailability)
        //			return BadRequest("Trainer is fully booked for one or more slot dates.");

        //		var schedule = new
        //		{
        //			ScheduleId = scheduleId,
        //			UserIds = userIdsString, // user IDs as comma-separated values
        //			TrainerId = plan.BoxingTrainerId,
        //			FirstSlot = slotDates.Min(d => d),
        //			LastSlot = slotDates.Max(d => d),
        //			SlotCount = slotCount
        //		};

        //		// Save schedule and slots to Firebase
        //		var jsonString = JsonSerializer.Serialize(schedule, options);
        //		await _firebaseClient.Child("Schedules").Child(scheduleId).PutAsync(jsonString);
        //		foreach (var slot in slots)
        //		{
        //			jsonString = JsonSerializer.Serialize(slot, options);
        //			await _firebaseClient.Child("Slots").PostAsync(jsonString);
        //		}
        //		return Ok("Schedule created successfully.");
        //	}

        //	else if (string.IsNullOrEmpty(request.TrainerRentalPlanId) && string.IsNullOrEmpty(request.BoxingMembershipPlanId))
        //	{
        //		return BadRequest("No membership or plan yet.");
        //	}
        //	else
        //	{
        //		return BadRequest("Invalid request.");
        //	}

        //	#region validate slots
        //	//// Validate slots based on the plan type
        //	//var validation = ValidateSlots(planType, request.Slots);

        //	//if (!validation.IsValid)
        //	//	return BadRequest(validation.ErrorMessage);
        //	#endregion
        //}

        //private async Task<bool> CheckTrainerAvailability(string trainerId, List<Slot> slots)
        //{
        //	var endSlotDate = slots.Max(s => s.Date);
        //	var startSlotDate = slots.Min(s => s.Date);

        //	// Group requested slots by date
        //	var slotsByDay = slots
        //		.GroupBy(s => s.Date)
        //		.Where(g => g.Key.DayOfWeek >= DayOfWeek.Monday && g.Key.DayOfWeek <= DayOfWeek.Friday) // Monday to Friday only
        //		.ToDictionary(g => g.Key, g => g.ToList());

        //	// Retrieve the trainer's existing schedules
        //	var trainerSchedules = await _firebaseClient
        //		.Child("Schedules")
        //		.OrderBy("trainerId")
        //		.EqualTo(trainerId)
        //		.OnceAsync<Schedule>();

        //	// Collect all the trainer's existing slots in the relevant date range
        //	List<Slot> trainerSlotsList = new List<Slot>();
        //	foreach (var schedule in trainerSchedules)
        //	{
        //		var trainerSlots = await _firebaseClient
        //			.Child("Slots")
        //			.OrderBy("scheduleId")
        //			.EqualTo(schedule.Object.ScheduleId)
        //			.OnceAsync<Slot>();

        //		trainerSlotsList.AddRange(
        //			trainerSlots.Select(sl => sl.Object)
        //			.Where(sl => sl.Date <= endSlotDate && sl.Date >= startSlotDate));
        //	}

        //	// Group trainer's existing slots by date (Monday to Friday only)
        //	var trainerSlotsByDay = trainerSlotsList
        //		.GroupBy(s => s.Date)
        //		.Where(g => g.Key.DayOfWeek >= DayOfWeek.Monday && g.Key.DayOfWeek <= DayOfWeek.Friday)
        //		.ToDictionary(g => g.Key, g => g.ToList());

        //	// Check availability for each requested date
        //	foreach (var requestedDay in slotsByDay)
        //	{
        //		var requestedDate = requestedDay.Key;
        //		var requestedSlots = requestedDay.Value;

        //		// Check if this date already has some slots scheduled
        //		if (trainerSlotsByDay.TryGetValue(requestedDate, out var existingSlots))
        //		{
        //			// Total slots for the day, including existing and requested ones
        //			int totalSlotsForDay = existingSlots.Count + requestedSlots.Count;

        //			// Ensure total slots do not exceed 8
        //			if (totalSlotsForDay > 8)
        //			{
        //				return false; // Exceeds allowed slots for the day
        //			}

        //			// Check for time conflicts within the requested slots
        //			foreach (var requestedSlot in requestedSlots)
        //			{
        //				if (existingSlots.Any(existingSlot => SlotsOverlap(existingSlot, requestedSlot)))
        //				{
        //					return false; // Conflicting slot found
        //				}
        //			}
        //		}
        //		else
        //		{
        //			// No existing slots on this day; just check if requested slots exceed the limit
        //			if (requestedSlots.Count > 1)
        //			{
        //				return false; // Exceeds allowed slots for the day
        //			}
        //		}
        //	}

        //	return true; // All requested slots are available
        //}

        //// Helper method to check if two slots overlap
        //private bool SlotsOverlap(Slot existingSlot, Slot requestedSlot)
        //{
        //	return existingSlot.StartTime < requestedSlot.EndTime && requestedSlot.StartTime < existingSlot.EndTime;
        //}

        //private List<DateOnly> GenerateSlotDates(DateOnly startDate, DateOnly endDate, bool isMonWedFri, bool isBoxing, int? sessions)
        //{
        //	//boxing
        //	if (isBoxing)
        //	{
        //		var dates = new List<DateOnly>();

        //		for (var date = startDate; sessions != 0; date = date.AddDays(1))
        //		{
        //			if (isMonWedFri && (date.DayOfWeek == DayOfWeek.Monday || date.DayOfWeek == DayOfWeek.Wednesday || date.DayOfWeek == DayOfWeek.Friday))
        //			{
        //				dates.Add(date);
        //				sessions--;
        //			}
        //			else if (!isMonWedFri && (date.DayOfWeek == DayOfWeek.Tuesday || date.DayOfWeek == DayOfWeek.Thursday || date.DayOfWeek == DayOfWeek.Saturday))
        //			{
        //				dates.Add(date);
        //				sessions--;
        //			}
        //		}

        //		return dates;
        //	}
        //	//trainer rental
        //	else
        //	{
        //		if (sessions.HasValue && sessions != 0)
        //		{
        //			var dates = new List<DateOnly>();

        //			for (var date = startDate; sessions != 0; date = date.AddDays(1))
        //			{
        //				if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
        //				{
        //					dates.Add(date);
        //					sessions--;
        //				}
        //			}
        //			return dates;
        //		}
        //		else
        //		{
        //			var dates = new List<DateOnly>();

        //			for (var date = startDate; date <= endDate; date = date.AddDays(1))
        //			{
        //				if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
        //				{
        //					dates.Add(date);
        //				}
        //			}
        //			return dates;
        //		}

        //	}
        //}

        //private (bool IsValid, string ErrorMessage) ValidateSlots(string planType, List<Slot> slots)
        //{
        //	// Group slots by both year and week number
        //	var slotsByWeek = slots
        //		.OrderBy(d => d.Date)
        //		.GroupBy(s => new
        //		{
        //			Year = s.Date.Year,
        //			Week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.Date.ToDateTime(s.StartTime), CalendarWeekRule.FirstDay, DayOfWeek.Monday)
        //		});

        //	// Find the first and last years in the slots
        //	var firstYear = slotsByWeek.FirstOrDefault()?.Key.Year ?? 0;
        //	var lastYear = slotsByWeek.LastOrDefault()?.Key.Year ?? 0;

        //	// Loop through each year within the range
        //	for (int year = firstYear; year <= lastYear; year++)
        //	{
        //		var slotsInYear = slotsByWeek
        //			.Where(g => g.Key.Year == year)
        //			.OrderBy(d => d.Key.Week)
        //			.ToList();

        //		if (planType == "TrainerRental")
        //		{
        //			// TrainerRental plan should have exactly 5 slots per full week
        //			foreach (var weekGroup in slotsInYear)
        //			{
        //				// Check if the current week has exactly 5 slots
        //				if (weekGroup.Count() != 5)
        //				{
        //					// Calculate all available weekdays from the first slots date until Friday
        //					var firstSlotDate = weekGroup.First().Date;

        //					if (firstSlotDate.DayOfWeek == DayOfWeek.Saturday || firstSlotDate.DayOfWeek == DayOfWeek.Sunday)
        //					{
        //						return (false, "Can not choose Saturday and Sunday.");
        //					}

        //					// Calculate the end of the week date, ensuring it lands on a Friday
        //					var fridayOfWeek = firstSlotDate
        //						.AddDays((int)DayOfWeek.Friday - (int)firstSlotDate.DayOfWeek);

        //					var mondayOfWeek = firstSlotDate
        //						.AddDays((int)DayOfWeek.Monday - (int)firstSlotDate.DayOfWeek);

        //					var isFullWeekDay = fridayOfWeek.Year == mondayOfWeek.Year;


        //					// If it's the last week of the year and not a full week, check if we can combine it with the first week of next year
        //					if (weekGroup.Key.Week == slotsInYear.Max(w => w.Key.Week) && !isFullWeekDay && weekGroup.Key.Year != slotsByWeek.Max(x => x.Key.Year))
        //					{
        //						// Get slots from the first week of the next year
        //						var nextYearSlots = slotsByWeek
        //							.Where(g => g.Key.Year == year + 1 && g.Key.Week == 1)
        //							.SelectMany(g => g)
        //							.ToList();

        //						// Combine slots from the last week of the current year with the first week of the next year
        //						var combinedWeekSlots = weekGroup.Concat(nextYearSlots).ToList();

        //						// Check if the combined slots form a full week
        //						if (combinedWeekSlots.Count == 5)
        //						{
        //							continue; // This combined week is valid, skip further checks for this week
        //						}
        //					}
        //					// Check if this is the first week of the year and needs to be combined with the last week of the previous year
        //					else if (weekGroup.Key.Week == 1 && year > firstYear && !isFullWeekDay)
        //					{
        //						var previousYearSlots = slotsByWeek
        //							.Where(g => g.Key.Year == year - 1 && g.Key.Week == slotsByWeek.Where(w => w.Key.Year == year - 1).Max(w => w.Key.Week))
        //							.SelectMany(g => g)
        //							.ToList();

        //						var combinedWeekSlots = previousYearSlots.Concat(weekGroup).ToList();
        //						if (combinedWeekSlots.Count == 5)
        //						{
        //							continue;
        //						}
        //					}
        //					// Check if this is the first week of the year and may be a partial week
        //					else if (weekGroup.Key.Week == slotsInYear.Min(w => w.Key.Week) && year == firstYear && weekGroup.Count() < 5)
        //					{
        //						// Generate the list of required weekdays between the first slot date and the calculated end of week (Friday)
        //						var requiredWeekdays = Enumerable.Range(0, fridayOfWeek.DayNumber - firstSlotDate.DayNumber)
        //							.Select(offset => firstSlotDate.AddDays(offset).DayOfWeek)
        //							.Where(d => d >= DayOfWeek.Monday && d <= DayOfWeek.Friday)
        //							.ToList();


        //						// Check if a slot is registered on each available weekday
        //						var registeredWeekdays = weekGroup.Select(s => s.Date.DayOfWeek).ToList();
        //						var missingWeekdays = requiredWeekdays.Except(registeredWeekdays).ToList();

        //						if (missingWeekdays.Any())
        //						{
        //							return (false, $"In the first partial week of {year}, slots are missing on these required weekdays: {string.Join(", ", missingWeekdays)}.");
        //						}

        //						continue;
        //					}

        //					// If not valid, return error message
        //					return (false, $"Trainer rental plan requires exactly 5 slots per week in year {year}.");
        //				}

        //				// Check that all slots fall on weekdays (Monday to Friday)
        //				if (!AreSlotsOnWeekdays(weekGroup))
        //				{
        //					return (false, "Trainer rental plan slots must occur only on weekdays (Monday to Friday).");
        //				}
        //			}
        //		}
        //		else if (planType == "Boxing")
        //		{
        //			// Boxing plan should have exactly 3 slots on Mon/Wed/Fri or Tue/Thu/Sat, and every week must follow the same pattern
        //			var firstWeek = slotsInYear.Find(w => w.Key.Week == slotsInYear.Min(w => w.Key.Week));

        //			if (firstWeek != null)
        //			{
        //				var daysInFirstWeek = firstWeek.Select(s => s.Date.DayOfWeek).OrderBy(d => d).ToList();
        //				var validDaysSet1 = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
        //				var validDaysSet2 = new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday };
        //				var validSet = new List<DayOfWeek>();

        //				if (daysInFirstWeek.Count == 3)
        //				{
        //					if (!(daysInFirstWeek.SequenceEqual(validDaysSet1) || daysInFirstWeek.SequenceEqual(validDaysSet2)))
        //					{
        //						return (false, "Boxing plan must have exactly 3 slots per week on either Mon/Wed/Fri or Tue/Thu/Sat, consistently.");
        //					}
        //					else if (daysInFirstWeek.SequenceEqual(validDaysSet1))
        //					{
        //						validSet = validDaysSet1;
        //					}
        //					else if (daysInFirstWeek.SequenceEqual(validDaysSet2))
        //					{
        //						validSet = validDaysSet2;
        //					}
        //				}

        //				else if (daysInFirstWeek.Count < 3)
        //				{
        //					// Ensure all days in this partial week belong to one of the allowed sets
        //					bool isValidPartialWeek = false;

        //					if (daysInFirstWeek.All(day => validDaysSet1.Contains(day)))
        //					{
        //						validSet = validDaysSet1;
        //						isValidPartialWeek = true;
        //					}

        //					if (daysInFirstWeek.All(day => validDaysSet2.Contains(day)))
        //					{
        //						validSet = validDaysSet2;
        //						isValidPartialWeek = true;
        //					}

        //					// Check that if only 2 days are chosen, they are adjacent in the pattern
        //					if (isValidPartialWeek && daysInFirstWeek.Count == 2)
        //					{
        //						bool isAdjacentInValidSet1 = daysInFirstWeek.SequenceEqual(new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday }) ||
        //													 daysInFirstWeek.SequenceEqual(new List<DayOfWeek> { DayOfWeek.Wednesday, DayOfWeek.Friday });

        //						bool isAdjacentInValidSet2 = daysInFirstWeek.SequenceEqual(new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday }) ||
        //													 daysInFirstWeek.SequenceEqual(new List<DayOfWeek> { DayOfWeek.Thursday, DayOfWeek.Saturday });

        //						if (!(isAdjacentInValidSet1 || isAdjacentInValidSet2))
        //						{
        //							return (false, "Partial weeks with 2 slots must have adjacent days within the same set: Mon/Wed, Wed/Fri, Tue/Thu, or Thu/Sat.");
        //						}
        //					}

        //					// Ensure that all slots in the partial week follow the chosen pattern, without skipping days within a set
        //					if (!isValidPartialWeek)
        //					{
        //						return (false, "Partial weeks must only contain slots on allowed days, and slots should be within a single set (Mon/Wed/Fri or Tue/Thu/Sat).");
        //					}

        //					// Check that a partial week with 1 slot is in one of the allowed days
        //					if (daysInFirstWeek.Count == 1)
        //					{
        //						if (!validDaysSet1.Contains(daysInFirstWeek[0]) && !validDaysSet2.Contains(daysInFirstWeek[0]))
        //						{
        //							return (false, "Single-day slots in a partial week must be on an allowed day (either Mon, Wed, Fri or Tue, Thu, Sat).");
        //						}
        //					}
        //				}

        //				else if (daysInFirstWeek.Count > 3)
        //				{
        //					return (false, "Boxing plan cannot have more than 3 slots per week.");
        //				}

        //				else
        //				{
        //					return (false, "Boxing plan must have exactly 3 slots per week on either Mon/Wed/Fri or Tue/Thu/Sat, consistently.");
        //				}

        //				// Validate each week to follow the established pattern
        //				foreach (var weekGroup in slotsInYear)
        //				{
        //					var daysInWeek = weekGroup.Select(s => s.Date.DayOfWeek).OrderBy(d => d).ToList();

        //					if (daysInWeek.Count == 3)
        //					{
        //						// Full week, must match the valid pattern exactly
        //						if (!daysInWeek.SequenceEqual(validSet))
        //						{
        //							return (false, $"All full weeks in year {year} must follow the same pattern: either Mon/Wed/Fri or Tue/Thu/Sat.");
        //						}
        //					}
        //					else
        //					{
        //						// Partial week: must only contain days from the valid pattern
        //						if (!daysInWeek.All(d => validSet.Contains(d)))
        //						{
        //							return (false, "Partial weeks must only contain sessions on allowed days based on the established pattern.");
        //						}
        //					}
        //				}

        //				// Get the date of the last session
        //				var lastSessionDate = slotsInYear.Max(w => w.Max(s => s.Date));

        //				// Calculate the start of the week for the last session
        //				var startOfLastWeek = lastSessionDate.AddDays(-(int)lastSessionDate.DayOfWeek + (int)DayOfWeek.Monday);

        //				// Check if the last week's sessions form a complete valid pattern
        //				var lastWeekSlots = slotsInYear.Last();
        //				var lastWeekDays = lastWeekSlots.Select(s => s.Date.DayOfWeek).OrderBy(d => d).ToList();

        //				// If the last week has fewer than 3 days, ensure all days align with the valid pattern
        //				if (lastWeekDays.Count < 3)
        //				{
        //					if (!lastWeekDays.All(day => validSet.Contains(day)))
        //					{
        //						return (false, "Sessions crossing into a new year must still follow the chosen pattern in partial weeks.");
        //					}
        //				}

        //				// Ensure no extra days in the last week that don’t align with the valid pattern
        //				if (lastWeekDays.Count > 3 || !lastWeekDays.SequenceEqual(validSet.Take(lastWeekDays.Count)))
        //				{
        //					return (false, "The final week's sessions do not align with the chosen pattern.");
        //				}

        //			}
        //		}
        //	}

        //	return (true, string.Empty);
        //}
        //// Helper method to check if all slots in a week are on weekdays
        //private bool AreSlotsOnWeekdays(IEnumerable<Slot> weekGroup)
        //{
        //	var weekdays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        //	return weekGroup.All(s => weekdays.Contains(s.Date.DayOfWeek));
        //}

    }
}
