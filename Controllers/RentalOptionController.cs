using Alpha_API.Models;
using Alpha_API.Services;
using Alpha_API.ViewModel;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class RentalOptionController : ControllerBase
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public RentalOptionController(FirebaseClient firebaseClient, FirebaseAuth firebaseAuth, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		// GET: api/RentalOption
		[HttpGet]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<IEnumerable<RentalOption>>> GetRentalOptions()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var options = await _firebaseClient
				.Child("RentalOptions")
				.OnceAsync<RentalOption>();

			foreach (var option in options)
			{
				option.Object.RentalOptionId = option.Key;
			}

			return options.Select(o => o.Object).ToList();
		}

		// GET: api/RentalOption/{id}
		[HttpGet("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<RentalOption>> GetRentalOption(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var option = await _firebaseClient
				.Child("RentalOptions")
				.Child(id)
				.OnceSingleAsync<RentalOption>();

			if (option == null)
			{
				return NotFound();
			}
			option.RentalOptionId = id;

			return option;
		}

		// POST: api/RentalOption
		[HttpPost]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<RentalOption>> CreateRentalOption(RentalOptionDto rentalOption)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Check if an option with similar parameters exists
			var existingOptions = await _firebaseClient
				.Child("RentalOptions")
				.OnceAsync<RentalOption>();

			if (existingOptions.Any(o =>
				o.Object.SessionCountMin == rentalOption.SessionCountMin &&
				o.Object.SessionCountMax == rentalOption.SessionCountMax &&
				o.Object.MemberCount == rentalOption.MemberCount &&
				o.Object.PricePerPersonPerSession == rentalOption.PricePerPersonPerSession &&
				o.Object.PricePerPersonPerMonth == rentalOption.PricePerPersonPerMonth))
			{
				return Conflict(new { message = "A similar rental option already exists." });
			}

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = System.Text.Json.JsonSerializer.Serialize(rentalOption, options);

			var result = await _firebaseClient
				.Child("RentalOptions")
				.PostAsync(rentalOption);

			return Ok();
		}

		// PATCH: api/RentalOption/{id}
		[HttpPatch("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> UpdateRentalOption(string id, RentalOption rentalOption)
		{
			return BadRequest("Goi api nay la hong het day");
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var existingOption = await _firebaseClient
				.Child("RentalOptions")
				.Child(id)
				.OnceSingleAsync<RentalOption>();

			if (existingOption == null)
			{
				return NotFound();
			}

			var updatedOption = new
			{
				rentalOption.Description,
				rentalOption.SessionCountMin,
				rentalOption.SessionCountMax,
				rentalOption.MemberCount,
				rentalOption.PricePerPersonPerSession,
				rentalOption.PricePerPersonPerMonth
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = JsonSerializer.Serialize(updatedOption, options);

			await _firebaseClient
				.Child("RentalOptions")
				.Child(id)
				.PatchAsync(jsonString);

			return NoContent();
		}

		// DELETE: api/RentalOption/{id}
		[HttpDelete("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> DeleteRentalOption(string id)
		{
			return BadRequest("Goi api nay la hong het day");
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var existingOption = await _firebaseClient
				.Child("RentalOptions")
				.Child(id)
				.OnceSingleAsync<RentalOption>();

			if (existingOption == null)
			{
				return NotFound();
			}

			await _firebaseClient
				.Child("RentalOptions")
				.Child(id)
				.DeleteAsync();

			return NoContent();
		}
	}
}
