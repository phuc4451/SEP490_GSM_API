using Alpha_API.Models;
using Alpha_API.Services;
using Alpha_API.Wrapper.Interfaces;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TrainerController : ControllerBase
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly ITrainerService _trainerService;

		public TrainerController(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider, ITrainerService trainerService)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			_trainerService = trainerService;
		}

		// PATCH: api/trainer/UpdateTrainer/{id}
		[Authorize(Roles = "admin,staff")]
		[HttpPatch("UpdateTrainer/{id}")]
		public async Task<ActionResult> UpdateTrainer(string id, [FromBody] Trainer trainer)
		{
			var result = await _trainerService.UpdateTrainerAsync(id, trainer);

			if (!result)
			{
				return Conflict("Error while update trainer");
			}

			return NoContent();
		}

		// GET: api/trainer/GetAllTrainers
		[Authorize(Roles = "admin,staff,customer")]
		[HttpGet("GetAllTrainersWithOptions")]
		public async Task<ActionResult<List<Object>>> GetAllTrainersWithOptions()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var result = await _trainerService.GetAllTrainersAsync();

			if (result == null)
			{
				return NotFound("No trainers found");
			}

			List<Object> list = new List<Object>();

			foreach (var trainer in result)
			{
				var boxingPlansTask = _firebaseClient.Child("BoxingMembershipPlans").OrderBy("boxingTrainerId").EqualTo(trainer.TrainerId).OnceAsync<BoxingMembershipPlan>();

				var rentalPlansTask = _firebaseClient.Child("TrainerRentalPlans").OrderBy("trainerId").EqualTo(trainer.TrainerId).OnceAsync<TrainerRentalPlan>();

				await Task.WhenAll(boxingPlansTask, rentalPlansTask);

				var boxingOptions = boxingPlansTask.Result.Select(plan => _firebaseClient.Child("BoxingOptions").Child(plan.Object.BoxingOptionId).OnceSingleAsync<BoxingOption>()).ToList();

				var boxingOptionsResult = await Task.WhenAll(boxingOptions);

				var rentalOptions = rentalPlansTask.Result.Select(plan => _firebaseClient.Child("RentalOptions").Child(plan.Object.RentalOptionId).OnceSingleAsync<RentalOption>()).ToList();

				var rentalOptionsResult = await Task.WhenAll(rentalOptions);

				var trainerWithOptions = new
				{
					trainer.Name,
					trainer.Bio,
					trainer.IsTrainerGym,
					trainer.IsTrainerBoxing,
					trainer.Specialization,
					trainer.TrainerId,
					trainer.UserId,
					BoxingOptions = boxingOptionsResult,
					Rentaloptions = rentalOptionsResult,
				};

				list.Add(trainerWithOptions);
			}

			return Ok(list);
		}

		// GET: api/trainer/GetTrainerById/{id}
		[Authorize(Roles = "admin,staff,customer")]
		[HttpGet("GetTrainerById/{id}")]
		public async Task<ActionResult<Trainer>> GetTrainerById(string id)
		{
			var result = await _trainerService.GetTrainerByIdAsync(id);

			if (result == null)
			{
				return NotFound("No trainers found");
			}

			return Ok(result);
		}

		// POST: api/trainer/AddTrainer
		[Authorize(Roles = "admin,staff")]
		[HttpPost("AddTrainer")]
		public async Task<ActionResult> AddTrainer(Trainer trainer)
		{
			var result = await _trainerService.AddTrainerAsync(trainer);

			return Ok();
		}

		// DELETE: api/trainer/DeleteTrainer
		[Authorize(Roles = "admin,staff")]
		[HttpDelete("DeleteTrainer")]
		public async Task<ActionResult> DeleteTrainer(string trainerId)
		{
			var result = await _trainerService.DeleteTrainerAsync(trainerId);

			if (!result)
			{
				return Conflict("Delete failed");
			}

			return Ok();
		}


	}
}
