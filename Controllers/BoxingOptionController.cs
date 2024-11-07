using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;
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

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BoxingOptionController : ControllerBase
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public BoxingOptionController(FirebaseClient firebaseClient, FirebaseAuth firebaseAuth, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		// GET: api/BoxingOption
		[HttpGet]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<IEnumerable<BoxingOption>>> GetBoxingOptions()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var options = await _firebaseClient
				.Child("BoxingOptions")
				.OnceAsync<BoxingOption>();


			foreach (var option in options)
			{
				option.Object.BoxingOptionId = option.Key;
			}

			return options.Select(m => m.Object).ToList();
		}

		// GET: api/BoxingOption/{id}
		[HttpGet("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<BoxingOption>> GetBoxingOption(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var option = await _firebaseClient
				.Child("BoxingOptions")
				.Child(id)
				.OnceSingleAsync<BoxingOption>();


			if (option == null)
			{
				return NotFound();
			}
			option.BoxingOptionId = id;

			return option;
		}

		// POST: api/BoxingOption
		[HttpPost]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<BoxingOption>> CreateBoxingOption(BoxingOption boxingOption)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Check if option with the same duration, session count, and price exists
			var existingMemberships = await _firebaseClient
				.Child("BoxingOptions")
				.OnceAsync<BoxingOption>();

			if (existingMemberships.Any(m =>
				m.Object.Months == boxingOption.Months &&
				m.Object.MemberCount == boxingOption.MemberCount &&
				m.Object.Sessions == boxingOption.Sessions &&
				m.Object.TotalPrice == boxingOption.TotalPrice))
			{
				return Conflict(new { message = "A similar boxing option already exists." });
			}

			var result = await _firebaseClient
				.Child("BoxingOptions")
				.PostAsync(new
				{
					description = boxingOption.Description,
					month = boxingOption.Months,
					memberCount = boxingOption.MemberCount,
					sessions = boxingOption.Sessions,
					totalPrice = boxingOption.TotalPrice,
				});

			boxingOption.BoxingOptionId = result.Key;

			return CreatedAtAction(nameof(GetBoxingOption), new { id = boxingOption.BoxingOptionId }, boxingOption);
		}

		// PUT: api/BoxingOption/{id}
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

		// DELETE: api/BoxingOption/{id}
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
