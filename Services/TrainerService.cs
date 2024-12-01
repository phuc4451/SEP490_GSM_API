using Alpha_API.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2016.Excel;

namespace Alpha_API.Services
{
	public class TrainerService
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly TimeSlotService _timeSlotService;


		public TrainerService(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider, TimeSlotService timeSlotService)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			_timeSlotService = timeSlotService;
		}

		// Add a new trainer
		public async Task<string> AddTrainerAsync(Trainer trainer)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			await _firebaseClient
				.Child("Trainers")
				.PostAsync(new
				{
					name = trainer.Name,
					userId = trainer.UserId,
					isTrainerBoxing = trainer.IsTrainerBoxing,
					isTrainerGym = trainer.IsTrainerGym,
					bio = trainer.Bio,
					specialization = trainer.Specialization
				});

			return trainer.Name;
		}

		// Get all trainers
		public async Task<List<Trainer>> GetAllTrainersAsync()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var trainers = await _firebaseClient
				.Child("Trainers")
				.OnceAsync<Trainer>();
			foreach (var trainer in trainers)
			{
				trainer.Object.TrainerId = trainer.Key;
			}

			return trainers.Select(t => t.Object).ToList();
		}

		// Get a trainer by ID
		public async Task<Trainer> GetTrainerByIdAsync(string trainerId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var trainer = await _firebaseClient
				.Child("Trainers")
				.Child(trainerId)
				.OnceSingleAsync<Trainer>();

			trainer.TrainerId = trainerId;

			return trainer;
		}

		// Update an existing trainer
		public async Task<bool> UpdateTrainerAsync(string id, Trainer trainer)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var existingTrainer = await GetTrainerByIdAsync(trainer.TrainerId);
			if (existingTrainer == null)
				return false;

			var addTrainer = new
			{
				name = trainer.Name,
				userId = trainer.UserId,
				isTrainerBoxing = trainer.IsTrainerBoxing,
				isTrainerGym = trainer.IsTrainerGym,
				bio = trainer.Bio,
				specialization = trainer.Specialization
			};

			await _firebaseClient
				.Child("Trainers")
				.Child(id)
				.PatchAsync(addTrainer);

			return true;
		}

		// Delete a trainer by ID
		public async Task<bool> DeleteTrainerAsync(string trainerId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var trainerExists = await GetTrainerByIdAsync(trainerId);
			if (trainerExists == null)
				return false;

			await _firebaseClient
				.Child("Trainers")
				.Child(trainerId)
				.DeleteAsync();

			return true;
		}

		public async Task<Slot> SlotAtTimeAsync(string trainerId, DateTime time)
		{
			var schedules = await _firebaseClient
					.Child("Schedules")
					.OrderBy("trainerId")
					.EqualTo(trainerId)
					.OnceAsync<Schedule>();

			// Create a dictionary to store slots grouped by their schedule
			var listSlotTasks = schedules.Select(async schedule =>
			{
				var slots = await _firebaseClient
					.Child("Slots")
					.OrderBy("scheduleId")
					.EqualTo(schedule.Key)
					.OnceAsync<Slot>();

				return slots.Select(slot => slot.Object).ToList();
			});

			// Fetch all slots in parallel
			var listSlot = (await Task.WhenAll(listSlotTasks)).SelectMany(slots => slots).ToList();

			// Convert the request time to DateOnly
			var requestDate = DateOnly.FromDateTime(time);

			// Filter slots for the requested date
			var slotsForDate = listSlot.Where(slot => slot.Date == requestDate).ToList();

			if (!slotsForDate.Any())
			{
				throw new InvalidOperationException("No slot today");
			}

			// Check if any slot is available now
			var currentSlot = slotsForDate.Select(slot =>
			{
				var timeSlot = _timeSlotService.GetTimeSlot(slot.SlotId).Split("-");
				var startTime = TimeOnly.ParseExact(timeSlot[0], "H:mm");
				var endTime = TimeOnly.ParseExact(timeSlot[1], "H:mm");

				var startDateTime = slot.Date.ToDateTime(startTime);
				var endDateTime = slot.Date.ToDateTime(endTime);

				if (time >= startDateTime && time <= endDateTime)
					return slot;
				else
					return null;
			});

			// Await all the tasks and filter out null results
			var validSlots = currentSlot.Where(slot => slot != null).ToList();

			// Ensure there’s only one valid shift
			if (validSlots.Count > 1)
			{
				throw new InvalidOperationException("Multiple valid slots found for the given checkTime.");
			}
			else if (validSlots.Count == 0)
			{
				return null;
			}
			// Return the valid shift or null if none found
			var foundSlot = validSlots.FirstOrDefault();

			return foundSlot;
		}

		public async Task<string> GetTrainerName(string trainerId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var name = await _firebaseClient
				.Child("Trainers")
				.Child(trainerId)
				.Child("name")
				.OnceSingleAsync<string>();

			return name;
		}

		public async Task AssignSalaryConfigToTrainerAsync(TrainerSalaryAssignment assignment)
		{
			try
			{
				// Initialize Firebase client (assuming it's being injected)
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				// Check if a shift with the same details already exists (ShiftName, StartTime, EndTime)
				var existingAssignment = await CheckForExistingSalaryAssignmentAsync(assignment);
				if (existingAssignment)
				{
					throw new InvalidOperationException("An assignment with the same details already exists.");
				}

				// Create a new configuration with a unique ID
				var id = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
				var newAssignment = new TrainerSalaryAssignment
				{
					TrainerId = assignment.TrainerId,
					ConfigurationId = assignment.ConfigurationId,
				};

				var options = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
				};

				var jsonString = JsonSerializer.Serialize(newAssignment, options);

				// Store the new configuration in Firebase
				await _firebaseClient.Child("TrainerSalaryAssignments").Child(id).PutAsync(jsonString);

				Console.WriteLine("TrainerSalaryAssignment created successfully.");
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error creating shift: {ex.Message}");
				throw new InvalidOperationException("An error occurred while creating the TrainerSalaryAssignment.", ex);
			}
		}

		private async Task<bool> CheckForExistingSalaryAssignmentAsync(TrainerSalaryAssignment assignment)
		{
			try
			{
				// Check for an existing shift with the same ShiftName, StartTime, and EndTime
				var existingAssignments = await _firebaseClient.Child("TrainerSalaryAssignments")
					.OnceAsync<TrainerSalaryAssignment>();

				// Look for shifts that match the provided details
				foreach (var existingAssignment in existingAssignments)
				{
					if (existingAssignment.Object.ConfigurationId == assignment.ConfigurationId
						&& existingAssignment.Object.TrainerId == assignment.TrainerId)
					{
						return true; // Return the existing shift if found
					}
				}

				return false; // No matching shift found
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				throw new Exception($"Error checking for existing shift: {ex.Message}");
			}
		}
	}
}
