using Alpha_API.Models;
using Alpha_API.Utils;
using Alpha_API.ViewModel;
using Firebase.Database;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Http;

namespace Alpha_API.Services
{
	public interface IScheduleService
	{
		Task<string> CreateSchedule(RegisterScheduleRequest request);
		Task<bool> CheckTrainerAvailability(string trainerId, List<Slot> slots);
	}

	public class ScheduleService : IScheduleService
	{
		private readonly EmailService _emailService;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public ScheduleService(EmailService emailService, FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider)
		{
			_emailService = emailService;
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		public async Task<string> CreateSchedule(RegisterScheduleRequest request)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			string planType = "";
			string trainerId = "";
			int numberOfUsers = request.Emails.Count;

			if (numberOfUsers == 0)
			{
				throw new InvalidOperationException("No emails in request");
			}

			if (string.IsNullOrEmpty(request.TrainerRentalPlanId) && string.IsNullOrEmpty(request.BoxingMembershipPlanId))
			{
				throw new InvalidOperationException("Memberships or plans are null");
			}

			List<string> userIds = new List<string>();
			StringBuilder userIdsBuilder = new StringBuilder();

			// Fetch user IDs in parallel
			var tasks = request.Emails.Select(async email =>
			{
				var userId = await _emailService.GetUserIdByEmail(email);
				return userId;
			});

			// Wait for all tasks to complete
			var userIdResults = await Task.WhenAll(tasks);

			// Filter out empty results (if the user was not found)
			userIds = userIdResults.Where(id => !string.IsNullOrEmpty(id)).ToList();


			//foreach (var email in request.Emails)
			//{
			//	var userId = await _emailService.GetUserIdByEmail(email);
			//	userIds.Add(userId);
			//}

			foreach (var userId in userIds)
				userIdsBuilder.Append(userId).Append(",");
			if (userIdsBuilder.Length > 0)
			{
				userIdsBuilder.Length--; // This removes the last comma
			}

			string userIdsString = userIdsBuilder.ToString();

			if (!string.IsNullOrEmpty(request.TrainerRentalPlanId))
			{
				planType = "TrainerRental";
				var plan = await _firebaseClient
					.Child("TrainerRentalPlans")
					.Child(request.TrainerRentalPlanId)
					.OnceSingleAsync<TrainerRentalPlan>();

				trainerId = plan.TrainerId;

				var option = await _firebaseClient
					.Child("RentalOptions")
					.Child(plan.RentalOptionId)
					.OnceSingleAsync<RentalOption>();
				option.RentalOptionId = plan.RentalOptionId;

				if (option.MemberCount != numberOfUsers)
				{
					throw new InvalidOperationException($"This plan is for {option.MemberCount} persons");
				}

				if (!request.Duration.HasValue || request.Duration == 0)
				{
					throw new InvalidOperationException("Please type months or sessions");
				}

				// Handle rental options for months
				if (option.SessionCountMax == 0 && option.SessionCountMin == 0)
				{
					return await HandleRentalOptionForMonths(request, plan, option, numberOfUsers, trainerId, plan.TrainerRentalPlanId, userIdsString);
				}
				else
				{
					// Handle rental options for sessions
					return await HandleRentalOptionForSessions(request, plan, option, numberOfUsers, trainerId, plan.TrainerRentalPlanId, userIdsString);
				}
			}
			else if (!string.IsNullOrEmpty(request.BoxingMembershipPlanId))
			{
				planType = "Boxing";

				var plan = await _firebaseClient
					.Child("BoxingMembershipPlans")
					.Child(request.BoxingMembershipPlanId)
					.OnceSingleAsync<BoxingMembershipPlan>();

				trainerId = plan.BoxingTrainerId;

				var option = await _firebaseClient
					.Child("BoxingOptions")
					.Child(plan.BoxingOptionId)
					.OnceSingleAsync<BoxingOption>();

				if (option.MemberCount != numberOfUsers)
				{
					throw new InvalidOperationException($"This plan is for {option.MemberCount} persons");
				}

				return await HandleBoxingMembershipPlan(request, plan, option, numberOfUsers, trainerId, plan.BoxingMembershipPlanId, userIdsString);
			}
			else
			{
				throw new InvalidOperationException("Invalid request.");
			}
		}

		private async Task<string> HandleRentalOptionForMonths(RegisterScheduleRequest request, TrainerRentalPlan plan, RentalOption option, int numberOfUsers, string trainerId, string rentalPlanId, string userIdsString)
		{
			int duration = request.Duration.Value;
			var startDate = DateOnly.FromDateTime(DateTime.Now);
			var endDate = startDate.AddDays(1).AddMonths(duration);
			var slotDates = GenerateSlotDates(startDate, endDate, request.IsMonWedFri, false, 0);

			var scheduleId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			var slots = new List<Slot>();
			int slotCount = 0;

			foreach (var date in slotDates)
			{
				var slot = new Slot
				{
					ScheduleId = scheduleId,
					Date = date,
					//StartTime = TimeOnly.ParseExact(times[0], "H:mm"),
					//EndTime = TimeOnly.ParseExact(times[1], "H:mm"),
					TimeSlotId= request.SelectedTimeSlotId,
					Attended = false
				};
				slots.Add(slot);
				slotCount++;
			}

			var trainerAvailability = await CheckTrainerAvailability(trainerId, slots);
			if (!trainerAvailability)
				throw new InvalidOperationException("Trainer is fully booked for one or more slot dates.");

			var schedule = new
			{
				ScheduleId = scheduleId,
				UserIds = userIdsString, // user IDs as comma-separated values
				TrainerId = plan.TrainerId,
				FirstSlot = slotDates.Min(d => d),
				LastSlot = slotDates.Max(d => d),
				SlotCount = slotCount
			};

			await SaveScheduleAndSlotsToFirebase(scheduleId, slots, schedule);
			return scheduleId;
		}

		private async Task<string> HandleRentalOptionForSessions(RegisterScheduleRequest request, TrainerRentalPlan plan, RentalOption option, int numberOfUsers, string trainerId, string rentalPlanId, string userIdsString)
		{
			if (request.Duration > option.SessionCountMax || request.Duration < option.SessionCountMin)
			{
				throw new InvalidOperationException($"The number of sessions registered are invalid with this rental plan");
			}

			int duration = request.Duration.Value;
			var startDate = DateOnly.FromDateTime(DateTime.Now);
			var endDate = startDate.AddDays(duration);
			var slotDates = GenerateSlotDates(startDate, endDate, request.IsMonWedFri, false, duration);

			var scheduleId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			var slots = new List<Slot>();
			int slotCount = 0;
			var times = request.SelectedTimeSlotId.Split('-');

			foreach (var date in slotDates)
			{
				var slot = new Slot
				{
					ScheduleId = scheduleId,
					Date = date,
					//StartTime = TimeOnly.ParseExact(times[0], "H:mm"),
					//EndTime = TimeOnly.ParseExact(times[1], "H:mm"),
					TimeSlotId = request.SelectedTimeSlotId,
					Attended = false
				};
				slots.Add(slot);
				slotCount++;
			}

			var trainerAvailability = await CheckTrainerAvailability(trainerId, slots);
			if (!trainerAvailability)
				throw new InvalidOperationException("Trainer is fully booked for one or more slot dates.");

			var schedule = new
			{
				ScheduleId = scheduleId,
				UserIds = userIdsString, // user IDs as comma-separated values
				TrainerId = plan.TrainerId,
				FirstSlot = slotDates.Min(d => d),
				LastSlot = slotDates.Max(d => d),
				SlotCount = slotCount
			};

			await SaveScheduleAndSlotsToFirebase(scheduleId, slots, schedule);
			return scheduleId;
		}

		private async Task<string> HandleBoxingMembershipPlan(RegisterScheduleRequest request, BoxingMembershipPlan plan, BoxingOption option, int numberOfUsers, string trainerId, string boxingMembershipPlanId, string userIdsString)
		{
			int duration = option.Sessions;
			var startDate = DateOnly.FromDateTime(DateTime.Now);
			var endDate = startDate.AddDays(1).AddMonths(1);
			var slotDates = GenerateSlotDates(startDate, endDate, request.IsMonWedFri, true, duration);

			var scheduleId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			var slots = new List<Slot>();
			int slotCount = 0;
			var times = request.SelectedTimeSlotId.Split('-');

			foreach (var date in slotDates)
			{
				var slot = new Slot
				{
					ScheduleId = scheduleId,
					Date = date,
					//StartTime = TimeOnly.ParseExact(times[0], "H:mm"),
					//EndTime = TimeOnly.ParseExact(times[1], "H:mm"),
					TimeSlotId = request.SelectedTimeSlotId,
					Attended = false
				};
				slots.Add(slot);
				slotCount++;
			}

			var trainerAvailability = await CheckTrainerAvailability(trainerId, slots);
			if (!trainerAvailability)
				throw new InvalidOperationException("Trainer is fully booked for one or more slot dates.");

			var schedule = new
			{
				ScheduleId = scheduleId,
				UserIds = userIdsString, // user IDs as comma-separated values
				TrainerId = plan.BoxingTrainerId,
				FirstSlot = slotDates.Min(d => d),
				LastSlot = slotDates.Max(d => d),
				SlotCount = slotCount
			};

			await SaveScheduleAndSlotsToFirebase(scheduleId, slots, schedule);
			return scheduleId;
		}

		private async Task SaveScheduleAndSlotsToFirebase(string scheduleId, List<Slot> slots, object schedule)
		{
			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				Converters = { new DateOnlyJsonConverter(), new TimeOnlyJsonConverter() } // Add the custom converter
			};


			// Serialize schedule and save it
			var scheduleJson = JsonSerializer.Serialize(schedule, options);
			var scheduleTask = _firebaseClient.Child("Schedules").Child(scheduleId).PutAsync(scheduleJson);

			// Create a dictionary to batch insert slots
			var slotsDictionary = slots.ToDictionary(slot => Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15), slot =>
				slot);

			// Serialize slots dictionary as a single batch insert
			var slotsJson = JsonSerializer.Serialize(slotsDictionary, options);
			var slotsTask = _firebaseClient.Child("Slots").PatchAsync(slotsJson);

			// Await both tasks concurrently
			await Task.WhenAll(scheduleTask, slotsTask);
		}

		private List<DateOnly> GenerateSlotDates(DateOnly startDate, DateOnly endDate, bool isMonWedFri, bool isBoxing, int? sessions)
		{
			//boxing
			if (isBoxing)
			{
				var dates = new List<DateOnly>();

				for (var date = startDate; sessions != 0; date = date.AddDays(1))
				{
					if (isMonWedFri && (date.DayOfWeek == DayOfWeek.Monday || date.DayOfWeek == DayOfWeek.Wednesday || date.DayOfWeek == DayOfWeek.Friday))
					{
						dates.Add(date);
						sessions--;
					}
					else if (!isMonWedFri && (date.DayOfWeek == DayOfWeek.Tuesday || date.DayOfWeek == DayOfWeek.Thursday || date.DayOfWeek == DayOfWeek.Saturday))
					{
						dates.Add(date);
						sessions--;
					}
				}

				return dates;
			}
			//trainer rental
			else
			{
				if (sessions.HasValue && sessions != 0)
				{
					var dates = new List<DateOnly>();

					for (var date = startDate; sessions != 0; date = date.AddDays(1))
					{
						if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
						{
							dates.Add(date);
							sessions--;
						}
					}
					return dates;
				}
				else
				{
					var dates = new List<DateOnly>();

					for (var date = startDate; date <= endDate; date = date.AddDays(1))
					{
						if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
						{
							dates.Add(date);
						}
					}
					return dates;
				}

			}
		}

		public async Task<bool> CheckTrainerAvailability(string trainerId, List<Slot> slots)
		{
			var endSlotDate = slots.Max(s => s.Date);
			var startSlotDate = slots.Min(s => s.Date);

			var slotsByDay = slots
				.GroupBy(s => s.Date)
				.Where(g => g.Key.DayOfWeek >= DayOfWeek.Monday && g.Key.DayOfWeek <= DayOfWeek.Friday)
				.ToDictionary(g => g.Key, g => g.ToList());

			var trainerSchedules = await _firebaseClient
				.Child("Schedules")
				.OrderBy("trainerId")
				.EqualTo(trainerId)
				.OnceAsync<Schedule>();

			//List<Slot> trainerSlotsList = new List<Slot>();
			//foreach (var schedule in trainerSchedules)
			//{
			//	var trainerSlots = await _firebaseClient
			//		.Child("Slots")
			//		.OrderBy("scheduleId")
			//		.EqualTo(schedule.Object.ScheduleId)
			//		.OnceAsync<Slot>();

			//	trainerSlotsList.AddRange(
			//		trainerSlots.Select(sl => sl.Object)
			//		.Where(sl => sl.Date <= endSlotDate && sl.Date >= startSlotDate));
			//}

			// Collect all schedule IDs to batch fetch slots
			var scheduleIds = trainerSchedules.Select(s => s.Object.ScheduleId).ToList();
			if (!scheduleIds.Any())
			{
				return true; // No existing schedules, trainer is available
			}

			// Batch fetch all slots for the trainer's schedules
			var trainerSlotsTasks = scheduleIds.Select(scheduleId =>
				_firebaseClient
					.Child("Slots")
					.OrderBy("scheduleId")
					.EqualTo(scheduleId)
					.OnceAsync<Slot>());
			var trainerSlotsResults = await Task.WhenAll(trainerSlotsTasks);

			// Flatten and filter slots to relevant dates
			var trainerSlotsList = trainerSlotsResults
				.SelectMany(result => result.Select(slot => slot.Object))
				.Where(sl => sl.Date <= endSlotDate && sl.Date >= startSlotDate)
				.ToList();

			var trainerSlotsByDay = trainerSlotsList
				.GroupBy(s => s.Date)
				.Where(g => g.Key.DayOfWeek >= DayOfWeek.Monday && g.Key.DayOfWeek <= DayOfWeek.Friday)
				.ToDictionary(g => g.Key, g => g.ToList());

			foreach (var requestedDay in slotsByDay)
			{
				var requestedDate = requestedDay.Key;
				var requestedSlots = requestedDay.Value;

				if (trainerSlotsByDay.TryGetValue(requestedDate, out var existingSlots))
				{
					int totalSlotsForDay = existingSlots.Count + requestedSlots.Count;

					if (totalSlotsForDay > 8)
					{
						return false; // Exceeds allowed slots for the day
					}

					foreach (var requestedSlot in requestedSlots)
					{
						if (existingSlots.Any(existingSlot => SlotsOverlap(existingSlot, requestedSlot)))
						{
							return false; // Conflicting slot found
						}
					}
				}
				else
				{
					if (requestedSlots.Count > 1)
					{
						return false; // Exceeds allowed slots for the day
					}
				}
			}

			return true;
		}

		private bool SlotsOverlap(Slot existingSlot, Slot requestedSlot)
		{
			return existingSlot.TimeSlotId == requestedSlot.TimeSlotId;
		}
	}

}
