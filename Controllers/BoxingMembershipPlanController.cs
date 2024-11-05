using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BoxingMembershipPlanController : ControllerBase
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public BoxingMembershipPlanController(FirebaseClient firebaseClient, FirebaseAuth firebaseAuth, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		// GET: api/BoxingMembershipPlan
		[HttpGet]
		[Authorize(Roles = "admin,staff,customer")]
		public async Task<ActionResult<IEnumerable<BoxingMembershipPlan>>> GetBoxingMembershipPlans()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var plans = await _firebaseClient
				.Child("BoxingMembershipPlans")
				.OnceAsync<BoxingMembershipPlan>();


			foreach (var plan in plans)
			{
				plan.Object.BoxingMembershipPlanId = plan.Key;
			}

			return plans.Select(m => m.Object).ToList();
		}

		// GET: api/BoxingMembershipPlan/{id}
		[HttpGet("{id}")]
		[Authorize(Roles = "admin,staff,customer")]
		public async Task<ActionResult<BoxingMembershipPlan>> GetBoxingMembershipPlan(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var plan = await _firebaseClient
				.Child("BoxingMembershipPlans")
				.Child(id)
				.OnceSingleAsync<BoxingMembershipPlan>();


			if (plan == null)
			{
				return NotFound();
			}
			plan.BoxingMembershipPlanId = id;

			return plan;
		}

		// POST: api/BoxingMembershipPlan
		[HttpPost]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<BoxingMembershipPlan>> CreateBoxingOption(BoxingMembershipPlan boxingMembershipPlan)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Check if option with the same duration, session count, and price exists
			var existingPlans = await _firebaseClient
				.Child("BoxingMembershipPlans")
				.OnceAsync<BoxingMembershipPlan>();

			if (existingPlans.Any(m =>
				m.Object.BoxingTrainerId == boxingMembershipPlan.BoxingTrainerId &&
				m.Object.BoxingOptionId == boxingMembershipPlan.BoxingOptionId))
			{
				return Conflict(new { message = "A similar boxing plan already exists." });
			}

			var trainer = await _firebaseClient
				.Child("Trainers")
				.Child(boxingMembershipPlan.BoxingTrainerId)
				.OnceSingleAsync<Trainer>();

			if (trainer == null)
			{
				return NotFound("Trainer not found");
			}

			if (trainer.TrainerTypeId != "id_of_boxing_trainer")
			{
				return BadRequest("Trainer is not boxer");
			}

			var result = await _firebaseClient
				.Child("BoxingMembershipPlans")
				.PostAsync(new
				{
					boxingTrainerId = boxingMembershipPlan.BoxingTrainerId,
					boxingOptionId = boxingMembershipPlan.BoxingOptionId,
				});

			boxingMembershipPlan.BoxingMembershipPlanId = result.Key;

			return CreatedAtAction(nameof(GetBoxingMembershipPlan), new { id = boxingMembershipPlan.BoxingMembershipPlanId }, boxingMembershipPlan);
		}

		// PUT: api/BoxingMembershipPlan/{id}
		[HttpPatch("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> UpdateBoxingOption(string id, GymMembership gymMembership)
		{
			return BadRequest("Goi api nay la hong het day");
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var existingMembership = await _firebaseClient
				.Child("GymMemberships")
				.Child(id)
				.OnceSingleAsync<GymMembership>();

			if (existingMembership == null)
			{
				return NotFound();
			}
			var membership = new
			{
				gymMembership.Name,
				gymMembership.Price,
				gymMembership.DurationMonths,
				gymMembership.SessionCount,
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = JsonSerializer.Serialize(membership, options);

			await _firebaseClient
				.Child("GymMemberships")
				.Child(id)
				.PatchAsync(jsonString);

			return NoContent();
		}

		// DELETE: api/BoxingMembershipPlan/{id}
		[HttpDelete("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> DeleteBoxingOption(string id)
		{
			return BadRequest("Goi api nay la hong het day");
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var existingMembership = await _firebaseClient
				.Child("GymMemberships")
				.Child(id)
				.OnceSingleAsync<GymMembership>();

			if (existingMembership == null)
			{
				return NotFound();
			}

			await _firebaseClient
				.Child("GymMemberships")
				.Child(id)
				.DeleteAsync();

			return NoContent();
		}
	}
}
