using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TrainerRentalPlanController : ControllerBase
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public TrainerRentalPlanController(FirebaseClient firebaseClient, FirebaseAuth firebaseAuth, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		// GET: api/TrainerRentalPlan
		[HttpGet]
		[Authorize(Roles = "admin,staff,customer")]
		public async Task<ActionResult<IEnumerable<Object>>> GetTrainerRentalPlans()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var plans = await _firebaseClient
				.Child("TrainerRentalPlans")
				.OnceAsync<TrainerRentalPlan>();

			foreach (var plan in plans)
			{
				plan.Object.TrainerRentalPlanId = plan.Key;
			}

			var listPlan = plans.Select(p => p.Object).ToList();
			var list = new List<Object>();
			foreach (var plan in listPlan)
			{
				var trainer = await _firebaseClient
					.Child("Trainers")
					.Child(plan.TrainerId)
					.OnceSingleAsync<Trainer>();

				var option = await _firebaseClient
					.Child("RentalOptions")
					.Child(plan.RentalOptionId)
					.OnceSingleAsync<RentalOption>();

				var pl = new
				{
					plan.TrainerRentalPlanId,
					trainerName=trainer.Name,
					option.Description,
					option.PricePerPersonPerSession,
					option.PricePerPersonPerMonth,
					option.SessionCountMax,
					option.SessionCountMin,
					option.MemberCount,
				};

				list.Add(pl);
			}
			return list;
		}

		// GET: api/TrainerRentalPlan/{id}
		[HttpGet("{id}")]
		[Authorize(Roles = "admin,staff,customer")]
		public async Task<ActionResult<TrainerRentalPlan>> GetTrainerRentalPlan(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var plan = await _firebaseClient
				.Child("TrainerRentalPlans")
				.Child(id)
				.OnceSingleAsync<TrainerRentalPlan>();

			if (plan == null)
			{
				return NotFound();
			}
			plan.TrainerRentalPlanId = id;

			return plan;
		}

		// POST: api/TrainerRentalPlan
		[HttpPost]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<TrainerRentalPlan>> CreateTrainerRentalPlan(TrainerRentalPlan trainerRentalPlan)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Check if similar plan exists
			var existingPlans = await _firebaseClient
				.Child("TrainerRentalPlans")
				.OnceAsync<TrainerRentalPlan>();

			if (existingPlans.Any(p =>
				p.Object.TrainerId == trainerRentalPlan.TrainerId &&
				p.Object.RentalOptionId == trainerRentalPlan.RentalOptionId))
			{
				return Conflict(new { message = "A similar rental plan already exists." });
			}

			var trainer = await _firebaseClient
				.Child("Trainers")
				.Child(trainerRentalPlan.TrainerId)
				.OnceSingleAsync<Trainer>();

			if (trainer == null)
			{
				return NotFound("Trainer not found");
			}

			if (!trainer.IsTrainerGym)
			{
				return BadRequest("This trainer is not training gym");
			}

			var result = await _firebaseClient
				.Child("TrainerRentalPlans")
				.PostAsync(new
				{
					trainerId = trainerRentalPlan.TrainerId,
					rentalOptionId = trainerRentalPlan.RentalOptionId,
				});

			trainerRentalPlan.TrainerRentalPlanId = result.Key;

			return CreatedAtAction(nameof(GetTrainerRentalPlan), new { id = trainerRentalPlan.TrainerRentalPlanId }, trainerRentalPlan);
		}

		// PUT: api/TrainerRentalPlan/{id}
		[HttpPatch("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> UpdateTrainerRentalPlan(string id, TrainerRentalPlan trainerRentalPlan)
		{
			return BadRequest("Goi api nay la hong het day");
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var existingPlan = await _firebaseClient
				.Child("TrainerRentalPlans")
				.Child(id)
				.OnceSingleAsync<TrainerRentalPlan>();

			if (existingPlan == null)
			{
				return NotFound();
			}

			var planData = new
			{
				trainerRentalPlan.TrainerId,
				trainerRentalPlan.RentalOptionId
			};

			await _firebaseClient
				.Child("TrainerRentalPlans")
				.Child(id)
				.PatchAsync(planData);

			return NoContent();
		}

		// DELETE: api/TrainerRentalPlan/{id}
		[HttpDelete("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> DeleteTrainerRentalPlan(string id)
		{
			return BadRequest("Goi api nay la hong het day");
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var existingPlan = await _firebaseClient
				.Child("TrainerRentalPlans")
				.Child(id)
				.OnceSingleAsync<TrainerRentalPlan>();

			if (existingPlan == null)
			{
				return NotFound();
			}

			await _firebaseClient
				.Child("TrainerRentalPlans")
				.Child(id)
				.DeleteAsync();

			return NoContent();
		}
	}
}
