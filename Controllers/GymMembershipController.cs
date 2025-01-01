using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Alpha_API.Models;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;
using System.Text.Json;
using Alpha_API.Services;

namespace GymManagementAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class GymMembershipController : ControllerBase
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public GymMembershipController(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		// GET: api/GymMembership
		[HttpGet]
		[Authorize(Roles = "admin,staff,customer")]
		public async Task<ActionResult<IEnumerable<GymMembership>>> GetGymMemberships()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var memberships = await _firebaseClient
				.Child("GymMemberships")
				.OnceAsync<GymMembership>();


			foreach (var membership in memberships)
			{
				membership.Object.GymMembershipId = membership.Key;
			}

			return memberships.Select(m => m.Object).ToList();
		}

		// GET: api/GymMembership/{id}
		[HttpGet("{id}")]
		[Authorize(Roles = "admin,staff,customer")]
		public async Task<ActionResult<GymMembership>> GetGymMembership(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var membership = await _firebaseClient
				.Child("GymMemberships")
				.Child(id)
				.OnceSingleAsync<GymMembership>();

			if (membership == null)
			{
				return NotFound();
			}
			membership.GymMembershipId = id;

			return membership;
		}

		// POST: api/GymMembership
		[HttpPost]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<GymMembership>> CreateGymMembership(GymMembership gymMembership)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Check if membership with the same duration, session count, and price exists
			var existingMemberships = await _firebaseClient
				.Child("GymMemberships")
				.OnceAsync<GymMembership>();

			if (existingMemberships.Any(m =>
				m.Object.DurationMonths == gymMembership.DurationMonths &&
				m.Object.SessionCount == gymMembership.SessionCount &&
				m.Object.Price == gymMembership.Price))
			{
				return Conflict(new { message = "A similar membership package already exists." });
			}

			var result = await _firebaseClient
				.Child("GymMemberships")
				.PostAsync(new
				{
					price = gymMembership.Price,
					durationMonths = gymMembership.DurationMonths,
					name = gymMembership.Name,
					sessionCount = gymMembership.SessionCount,
				});

			gymMembership.GymMembershipId = result.Key;

			return CreatedAtAction(nameof(GetGymMembership), new { id = gymMembership.GymMembershipId }, gymMembership);
		}

		// PUT: api/GymMembership/{id}
		[HttpPatch("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> UpdateGymMembership(string id, GymMembership gymMembership)
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

		// DELETE: api/GymMembership/{id}
		[HttpDelete("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> DeleteGymMembership(string id)
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
