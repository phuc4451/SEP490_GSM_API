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

            // Fetch schedules related to the trainer
            var schedules = await _firebaseClient
                .Child("Schedules")
                .OrderBy("trainerId")
                .EqualTo(trainerId)
                .OnceAsync<Schedule>();

            if (!schedules.Any())
                return NotFound("No schedules found for this trainer.");

            // Extract schedule IDs
            var scheduleIds = schedules.Select(s => s.Key).ToList();

            // Fetch all relevant slots in one call
            var slots = (await _firebaseClient
                .Child("Slots")
                .OnceAsync<Slot>())
                .Where(s => scheduleIds.Contains(s.Object.ScheduleId))
                .ToList();

            if (!slots.Any())
                return NotFound("No slots found for this trainer's schedules.");

            // Collect user IDs from schedules
            var userIds = schedules
                .SelectMany(s => s.Object.UserIds?.Split(',') ?? Array.Empty<string>())
                .Distinct()
                .ToList();

            // Fetch all necessary users in bulk
            var userDict = (await Task.WhenAll(userIds.Select(id =>
                _firebaseClient.Child("users").Child(id).OnceSingleAsync<User>()
            )))
            .Where(user => user != null)
            .ToDictionary(u => u.UserId);

            // Fetch all time slots in bulk
            var timeSlotIds = slots.Select(s => s.Object.TimeSlotId).Distinct();
            var timeSlotDict = timeSlotIds.ToDictionary(
                id => id,
                id => _timeSlotService.GetTimeSlot(id)
            );

            // Group schedules by date and time slot
            var groupedSchedules = slots
                .GroupBy(slot => slot.Object.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(
                    dateGroup => dateGroup.Key,
                    dateGroup => dateGroup
                        .GroupBy(slot => timeSlotDict.GetValueOrDefault(slot.Object.TimeSlotId)?.ToString() ?? "Unknown")
                        .ToDictionary(
                            timeSlotGroup => timeSlotGroup.Key,
                            timeSlotGroup => timeSlotGroup
                                .SelectMany(slot => schedules
                                    .FirstOrDefault(s => s.Key == slot.Object.ScheduleId)?
                                    .Object.UserIds?
                                    .Split(',')
                                    .Where(userId => userDict.ContainsKey(userId))
                                    .Select(userId => userDict[userId].Name) ?? Array.Empty<string>()
                                )
                                .ToList()
                        )
                );

            // Flatten the result into a list of objects
            var result = groupedSchedules
                .SelectMany(dateGroup => dateGroup.Value
                    .Select(timeSlotGroup => new
                    {
                        date = dateGroup.Key,
                        timeSlot = timeSlotGroup.Key,
                        customers = timeSlotGroup.Value
                    }))
                .ToList();

            if (!result.Any())
                return NotFound("No customers found for this trainer's schedules.");

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

            // Map dữ liệu từ Firebase thành danh sách TimeSlot và sắp xếp theo Time
            var result = timeSlots
                .Select(ts => new TimeSlot
                {
                    TimeSlotId = ts.Key,
                    Time = ts.Object.Time
                })
                .OrderBy(ts => TimeSpan.Parse(ts.Time.Split('-')[0])) // Sắp xếp theo giờ bắt đầu
                .ToList();

            return result;
        }


        [HttpGet("Slots/All")]
        public async Task<ActionResult<IEnumerable<Object>>> GetAllTrainersSchedules(DateTime? inputDate)
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

            // Fetch all trainers
            var trainersTask = _firebaseClient.Child("Trainers").OnceAsync<Trainer>();
            // Fetch all schedules
            var schedulesTask = _firebaseClient.Child("Schedules").OnceAsync<Schedule>();
            // Fetch all slots
            var slotsTask = _firebaseClient.Child("Slots").OnceAsync<Slot>();
            // Fetch all users (customers)
            var usersTask = _firebaseClient.Child("users").OnceAsync<User>();
            // Fetch TrainerRentalRegistrations and BoxingRegistrations
            var trainerRentalRegistrationsTask = _firebaseClient.Child("TrainerRentalRegistrations").OnceAsync<TrainerRentalRegistration>();
            var boxingRegistrationsTask = _firebaseClient.Child("BoxingRegistrations").OnceAsync<BoxingRegistration>();
            // Fetch TrainerRentalPlans and BoxingMembershipPlans
            var trainerRentalPlansTask = _firebaseClient.Child("TrainerRentalPlans").OnceAsync<TrainerRentalPlan>();
            var boxingMembershipPlansTask = _firebaseClient.Child("BoxingMembershipPlans").OnceAsync<BoxingMembershipPlan>();
            // Fetch RentalOptions and BoxingOptions
            var rentalOptionsTask = _firebaseClient.Child("RentalOptions").OnceAsync<RentalOption>();
            var boxingOptionsTask = _firebaseClient.Child("BoxingOptions").OnceAsync<BoxingOption>();

            // Wait for all queries to complete
            await Task.WhenAll(trainersTask, schedulesTask, slotsTask, usersTask, trainerRentalRegistrationsTask, boxingRegistrationsTask, trainerRentalPlansTask, boxingMembershipPlansTask, rentalOptionsTask, boxingOptionsTask);

            var trainers = trainersTask.Result;
            var schedules = schedulesTask.Result;
            var slots = slotsTask.Result;
            var users = usersTask.Result;
            var trainerRentalRegistrations = trainerRentalRegistrationsTask.Result;
            var boxingRegistrations = boxingRegistrationsTask.Result;
            var trainerRentalPlans = trainerRentalPlansTask.Result;
            var boxingMembershipPlans = boxingMembershipPlansTask.Result;
            var rentalOptions = rentalOptionsTask.Result;
            var boxingOptions = boxingOptionsTask.Result;

            // Group schedules by trainer
            var groupedSchedules = trainers
                .GroupJoin(schedules,
                    trainer => trainer.Key, // Tìm TrainerId
                    schedule => schedule.Object.TrainerId, // Tìm các lịch theo TrainerId
                    (trainer, scheduleGroup) => new
                    {
                        TrainerId = trainer.Key,
                        TrainerName = trainer.Object.Name,
                        Schedules = scheduleGroup.ToList() // Chuyển nhóm schedule thành danh sách
                    })
                .ToList();

            // Now build the result by slot, including customers and rental/boxing options
            var result = new List<object>();

            foreach (var trainerGroup in groupedSchedules)
            {
                var trainerName = trainerGroup.TrainerName;
                var trainerId = trainerGroup.TrainerId;

                // Group by date for the trainer, filtering by date if provided
                var groupedByDate = slots
                    .Where(slot => slot.Object.ScheduleId != null && slot.Object.ScheduleId != "" && trainerGroup.Schedules.Any(s => s.Key == slot.Object.ScheduleId))
                    .Where(slot => !inputDate.HasValue || slot.Object.Date == DateOnly.FromDateTime(inputDate.Value)) // Chuyển inputDate thành DateOnly
                    .GroupBy(slot => slot.Object.Date)
                    .ToList();


                var trainerSlots = new List<object>();

                foreach (var dateGroup in groupedByDate)
                {
                    var date = dateGroup.Key;
                    var slotsForDate = dateGroup.ToList();

                    foreach (var slot in slotsForDate)
                    {
                        var timeSlot = _timeSlotService.GetTimeSlot(slot.Object.TimeSlotId); // Assuming this gets the time slot info

                        // Get the customers (userIds) for the slot
                        var customersIds = schedules
                            .Where(s => s.Key == slot.Object.ScheduleId) // Find the schedule for the slot
                            .SelectMany(s => s.Object.UserIds?.Split(',') ?? Array.Empty<string>()) // Get all userIds (customers) from the schedule
                            .ToList();

                        // Fetch user details for customers (names) from Users
                        var customerDetails = users
                            .Where(u => customersIds.Contains(u.Key))  // Filter users by the customerIds (userIds)
                            .Select(u => new
                            {
                                u.Object.Name
                            })
                            .ToList();

                        // Lookup Rental or Boxing Registration to get the planId
                        var planId = string.Empty;
                        var rentalOptionId = string.Empty;
                        var boxingOptionId = string.Empty;

                        // Check if it's a TrainerRentalRegistration
                        var trainerRentalRegistration = trainerRentalRegistrations
                            .FirstOrDefault(tr => tr.Object.ScheduleId == slot.Object.ScheduleId);

                        if (trainerRentalRegistration != null)
                        {
                            planId = trainerRentalRegistration.Object.PlanId;
                            var rentalPlan = trainerRentalPlans
                                .FirstOrDefault(rp => rp.Key == planId);

                            if (rentalPlan != null)
                            {
                                rentalOptionId = rentalPlan.Object.RentalOptionId;
                            }
                        }
                        else
                        {
                            // Check if it's a BoxingRegistration
                            var boxingRegistration = boxingRegistrations
                                .FirstOrDefault(br => br.Object.ScheduleId == slot.Object.ScheduleId);

                            if (boxingRegistration != null)
                            {
                                planId = boxingRegistration.Object.BoxingMembershipPlanId;
                                var boxingPlan = boxingMembershipPlans
                                    .FirstOrDefault(bp => bp.Key == planId);

                                if (boxingPlan != null)
                                {
                                    boxingOptionId = boxingPlan.Object.BoxingOptionId;
                                }
                            }
                        }

                        // Lookup descriptions from RentalOptions or BoxingOptions
                        var rentalOptionDescription = rentalOptions
                            .Where(ro => ro.Key == rentalOptionId) // Assuming RentalOptionId is part of RentalPlans
                            .Select(ro => ro.Object.Description)
                            .FirstOrDefault();

                        var boxingOptionDescription = boxingOptions
                            .Where(bo => bo.Key == boxingOptionId) // Assuming BoxingOptionId is part of BoxingPlans
                            .Select(bo => bo.Object.Description)
                            .FirstOrDefault();

                        // Add the slot details along with customers and options to the result
                        trainerSlots.Add(new
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            TimeSlot = timeSlot,
                            Customers = customerDetails.Any()
                                ? customerDetails.Cast<object>().ToList()  // Convert customerDetails to List<object>
                                : new List<object> { new { Message = "No customers" } },
                            RentalOption = rentalOptionDescription ?? "No rental option",
                            BoxingOption = boxingOptionDescription ?? "No boxing option"
                        });
                    }

                    // If the trainer has no slots for the day, we still show the date but with an empty customer list
                    if (!trainerSlots.Any())
                    {
                        trainerSlots.Add(new
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            TimeSlot = "No scheduled time slots",
                            Customers = new List<string> { "No customers" },
                            RentalOption = "No rental option",
                            BoxingOption = "No boxing option"
                        });
                    }
                }

                // If the trainer has no slots at all, we return a message
                if (!trainerSlots.Any())
                {
                    result.Add(new
                    {
                        TrainerName = trainerName,
                        Schedules = "No slots found for this trainer"
                    });
                }
                else
                {
                    result.Add(new
                    {
                        TrainerName = trainerName,
                        Slots = trainerSlots
                    });
                }
            }

            // Return the result
            return result;
        }



    }
}
