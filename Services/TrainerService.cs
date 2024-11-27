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

namespace Alpha_API.Services
{
	public class TrainerService
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;


		public TrainerService(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
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
		public async Task<bool> UpdateTrainerAsync(string id,Trainer trainer)
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
	}
}
