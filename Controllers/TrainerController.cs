using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Alpha_API.Services;
using Alpha_API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TrainerController : ControllerBase
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly TrainerService _trainerService;

		public TrainerController(FirebaseAuth firebaseAuth, FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider, TrainerService trainerService)
		{
			_firebaseAuth = firebaseAuth;
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
		[HttpGet("GetAllTrainers")]
		public async Task<ActionResult<List<Trainer>>> GetAllTrainers()
		{
			var result = await _trainerService.GetAllTrainersAsync();

			if (result == null)
			{
				return NotFound("No trainers found");
			}

			return Ok(result);
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
