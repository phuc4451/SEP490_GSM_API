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
		Task<(string, DateOnly)> CreateSchedule(RegisterScheduleRequest request, string userIdsString);
		Task<bool> CheckTrainerAvailability(string trainerId, List<Slot> slots);
	}

	public class ScheduleService : IScheduleService
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public ScheduleService(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		public async Task<(string, DateOnly)> CreateSchedule(RegisterScheduleRequest request, string userIdsString)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			int numberOfUsers = request.Emails.Count;

			if (numberOfUsers == 0)
			{
				throw new InvalidOperationException("No emails in request");
			}

			if (string.IsNullOrEmpty(request.TrainerRentalPlanId) && string.IsNullOrEmpty(request.BoxingMembershipPlanId))
			{
				throw new InvalidOperationException("Memberships or plans are null");
			}

			if (!string.IsNullOrEmpty(request.TrainerRentalPlanId))
			{
				var plan = await _firebaseClient
					.Child("TrainerRentalPlans")
					.Child(request.TrainerRentalPlanId)
					.OnceSingleAsync<TrainerRentalPlan>();

				if (plan == null)
				{
					throw new InvalidOperationException($"Can not find this TrainerRentalPlanId: {request.TrainerRentalPlanId}");
				}

				var trainerId = plan.TrainerId;

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

				var plan = await _firebaseClient
					.Child("BoxingMembershipPlans")
					.Child(request.BoxingMembershipPlanId)
					.OnceSingleAsync<BoxingMembershipPlan>();

				if (plan == null)
				{
					throw new InvalidOperationException($"Can not find this BoxingMembershipPlanId: {request.BoxingMembershipPlanId}");
				}

				var trainerId = plan.BoxingTrainerId;

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

		private async Task<(string, DateOnly)> HandleRentalOptionForMonths(RegisterScheduleRequest request, TrainerRentalPlan plan, RentalOption option, int numberOfUsers, string trainerId, string rentalPlanId, string userIdsString)
		{
			int duration = request.Duration.Value;
			var startDate = DateOnly.FromDateTime(DateTime.Now);
			var endDate = startDate.AddMonths(duration);
			var slotDates = GenerateSlotDates(startDate, endDate, request.IsMonWedFri, false, 0);

			var scheduleId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			var slots = new List<Slot>();

			slots.AddRange(slotDates.Select(date => new Slot
			{
				ScheduleId = scheduleId,
				Date = date,
				TimeSlotId = request.SelectedTimeSlotId,
				Attended = false
			}));

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
				SlotCount = slots.Count,
			};

			await SaveScheduleAndSlotsToFirebase(scheduleId, slots, schedule);
			return (scheduleId,schedule.LastSlot);
		}

		private async Task<(string, DateOnly)> HandleRentalOptionForSessions(RegisterScheduleRequest request, TrainerRentalPlan plan, RentalOption option, int numberOfUsers, string trainerId, string rentalPlanId, string userIdsString)
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

			slots.AddRange(slotDates.Select(date => new Slot
			{
				ScheduleId = scheduleId,
				Date = date,
				TimeSlotId = request.SelectedTimeSlotId,
				Attended = false
			}));

			var trainerAvailability = await CheckTrainerAvailability(trainerId, slots);
			if (!trainerAvailability)
				throw new InvalidOperationException("Trainer is fully booked for one or more slot dates.");

			var schedule = new
			{
				//ScheduleId = scheduleId,
				UserIds = userIdsString, // user IDs as comma-separated values
				TrainerId = plan.TrainerId,
				FirstSlot = slotDates.Min(d => d),
				LastSlot = slotDates.Max(d => d),
				SlotCount = slotDates.Count
			};

			await SaveScheduleAndSlotsToFirebase(scheduleId, slots, schedule);
			return (scheduleId, schedule.LastSlot);
		}

		private async Task<(string, DateOnly)> HandleBoxingMembershipPlan(RegisterScheduleRequest request, BoxingMembershipPlan plan, BoxingOption option, int numberOfUsers, string trainerId, string boxingMembershipPlanId, string userIdsString)
		{
			int duration = option.Sessions;
			var startDate = DateOnly.FromDateTime(DateTime.Now);
			var endDate = startDate.AddDays(1).AddMonths(1);
			var slotDates = GenerateSlotDates(startDate, endDate, request.IsMonWedFri, true, duration);

			var scheduleId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			var slots = new List<Slot>();

			slots.AddRange(slotDates.Select(date => new Slot
			{
				ScheduleId = scheduleId,
				Date = date,
				TimeSlotId = request.SelectedTimeSlotId,
				Attended = false
			}));

			var trainerAvailability = await CheckTrainerAvailability(trainerId, slots);
			if (!trainerAvailability)
				throw new InvalidOperationException("Trainer is fully booked for one or more slot dates.");

			var schedule = new
			{
				//ScheduleId = scheduleId,
				UserIds = userIdsString, // user IDs as comma-separated values
				TrainerId = plan.BoxingTrainerId,
				FirstSlot = slotDates.Min(d => d),
				LastSlot = slotDates.Max(d => d),
				SlotCount = slots.Count,
			};

			await SaveScheduleAndSlotsToFirebase(scheduleId, slots, schedule);
			return (scheduleId, schedule.LastSlot);
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
			var jsonString = JsonSerializer.Serialize(schedule, options);
			var scheduleTask = _firebaseClient.Child("Schedules").Child(scheduleId).PutAsync(jsonString);

			// Create a dictionary to batch insert slots
			var slotsDictionary = slots.ToDictionary(slot => Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15), slot =>
				slot);

			// Serialize slots dictionary as a single batch insert
			jsonString = JsonSerializer.Serialize(slotsDictionary, options);
			var slotsTask = _firebaseClient.Child("Slots").PatchAsync(jsonString);

			// Await both tasks concurrently
			await Task.WhenAll(scheduleTask, slotsTask);
		}

		private List<DateOnly> GenerateSlotDates(DateOnly startDate, DateOnly endDate, bool isMonWedFri, bool isBoxing, int? sessions)
		{
			var dates = new List<DateOnly>();

			if (isBoxing)
			{
				// Determine the valid days based on isMonWedFri
				var validDays = isMonWedFri
					? new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }
					: new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday };

				// Loop through the dates and add valid ones until sessions are exhausted
				for (var date = startDate; sessions > 0; date = date.AddDays(1))
				{
					if (Array.Exists(validDays, day => day == date.DayOfWeek))
					{
						dates.Add(date);
						sessions--;
					}
				}
			}
			else
			{
				// Handle trainer rental
				if (sessions.HasValue && sessions > 0) // Session-based
				{
					for (var date = startDate; sessions > 0; date = date.AddDays(1))
					{
						if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
						{
							dates.Add(date);
							sessions--;
						}
					}
				}
				else // Month-based
				{
					for (var date = startDate; date <= endDate; date = date.AddDays(1))
					{
						if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
						{
							dates.Add(date);
						}
					}
				}
			}

			return dates;

		}

		public async Task<bool> CheckTrainerAvailability(string trainerId, List<Slot> slots)
		{
			var startSlotDate = slots.Min(s => s.Date);
			var endSlotDate = slots.Max(s => s.Date);

			// Group requested slots by date
			var slotsByDay = slots
				.GroupBy(s => s.Date)
				.ToDictionary(g => g.Key, g => g.ToList());

			// Fetch trainer schedules
			var trainerSchedules = await _firebaseClient
				.Child("Schedules")
				.OrderBy("trainerId")
				.EqualTo(trainerId)
				.OnceAsync<Schedule>();

			var scheduleIds = trainerSchedules.Select(s => s.Key).ToList();
			if (!scheduleIds.Any())
				return true; // No schedules, trainer is available

			// Fetch all slots for the trainer's schedules in a batch
			var trainerSlotsResults = await Task.WhenAll(scheduleIds.Select(scheduleId =>
				_firebaseClient
					.Child("Slots")
					.OrderBy("scheduleId")
					.EqualTo(scheduleId)
					.OnceAsync<Slot>()));

			// Filter and flatten the results to relevant slots
			var trainerSlotsByDay = trainerSlotsResults
				.SelectMany(result => result.Select(slot => slot.Object))
				.Where(slot => slot.Date >= startSlotDate && slot.Date <= endSlotDate)
				.GroupBy(slot => slot.Date)
				.ToDictionary(g => g.Key, g => g.ToList());

			// Validate requested slots
			foreach (var (requestedDate, requestedSlots) in slotsByDay)
			{
				if (trainerSlotsByDay.TryGetValue(requestedDate, out var existingSlots))
				{
					// Check if total slots exceed the daily limit
					if (existingSlots.Count + requestedSlots.Count > 8)
						return false;

					// Check for overlapping slots
					if (requestedSlots.Any(requestedSlot =>
						existingSlots.Any(existingSlot => SlotsOverlap(existingSlot, requestedSlot))))
						return false;
				}
				else if (requestedSlots.Count > 1)
				{
					// New day but exceeds allowed slots for the day
					return false;
				}
			}

			return true;
		}

		private bool SlotsOverlap(Slot existingSlot, Slot requestedSlot)
		{
			return requestedSlot.TimeSlotId.Equals(existingSlot.TimeSlotId);
		}
	}

}
