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
using Alpha_API.Services;
using System.Security.Claims;

namespace GymManagementAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class GymRegistrationController : ControllerBase
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly RegisterService _registerService;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public GymRegistrationController(RegisterService registerService, FirebaseClientProvider firebaseClientProvider, FirebaseClient firebaseClient, FirebaseAuth firebaseAuth)
		{
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_registerService = registerService;
			_firebaseClientProvider = firebaseClientProvider;
		}

		// GET: api/GymRegistration
		[HttpGet]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<IEnumerable<GymRegistration>>> GetRegistrations()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var registrations = await _firebaseClient
				.Child("GymRegistrations")
				.OnceAsync<GymRegistration>();

			return registrations.Select(r => r.Object).ToList();
		}

		// GET: api/GymRegistration/{id}
		[HttpGet("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<GymRegistration>> GetRegistration(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var registration = await _firebaseClient
				.Child("GymRegistrations")
				.Child(id)
				.OnceSingleAsync<GymRegistration>();

			if (registration == null)
			{
				return NotFound();
			}

			return registration;
		}

		// GET: api/GymRegistration/{id}
		[HttpGet("MyRegistration")]
		[Authorize(Roles = "customer")]
		public async Task<ActionResult<GymRegistration>> MyRegistration()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Retrieve the uid claim
			var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

			if (userIdClaim == null)
			{
				return Unauthorized("User not authenticated.");
			}

			var userId = userIdClaim.Value; // This is the Firebase UID

			var registration = await _firebaseClient
				.Child("GymRegistrations")
				.OrderBy("userId")
				.EqualTo(userId)
				.OnceSingleAsync<GymRegistration>();

			if (registration == null)
			{
				return NotFound();
			}

			return registration;
		}

		// POST: api/GymRegistration
		[HttpPost]
		[Authorize(Roles = "admin,staff")] //customer can only pay qr =>call qrcontroller
		public async Task<ActionResult<GymRegistration>> CreateRegistration(RegisterRequest request)
		{
			var registerService = await _registerService.RegisterGym(request, request.QRPayment);

			return CreatedAtAction(nameof(GetRegistration), new { id = registerService.Registration.Key }, registerService.Registration);
		}

		// POST: api/GymRegistration/{id}
		[HttpPatch("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> ToggleRegistration(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var existingRegistration = await _firebaseClient
	.Child("GymRegistrations")
	.Child(id)
	.OnceSingleAsync<GymRegistration>();

			if (existingRegistration == null)
			{
				return NotFound();
			}
			bool isActive = true;
			if (existingRegistration.IsActive)
			{
				isActive = false;
			}
			else
			{
				isActive = true;
			}
			// Inactivate the registration
			await _firebaseClient
				.Child("GymRegistrations")
				.Child(id)
				.PatchAsync(new
				{
					isActive = isActive
				});

			return NoContent();
		}

		// PUT: api/GymRegistration/{id}
		[HttpPut("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> UpdateRegistration(string id, GymRegistration registration)
		{
			return BadRequest("Goi api nay la hong day");
			var existingRegistration = await _firebaseClient
				.Child("GymRegistrations")
				.Child(id)
				.OnceSingleAsync<GymRegistration>();

			if (existingRegistration == null)
			{
				return NotFound();
			}

			// Update the registration
			await _firebaseClient
				.Child("GymRegistrations")
				.Child(id)
				.PutAsync(registration);

			return NoContent();
		}

		// DELETE: api/GymRegistration/{id}
		[HttpDelete("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> DeleteRegistration(string id)
		{
			return BadRequest("Goi api nay la hong day");
			var existingRegistration = await _firebaseClient
				.Child("GymRegistrations")
				.Child(id)
				.OnceSingleAsync<GymRegistration>();

			if (existingRegistration == null)
			{
				return NotFound();
			}

			// Delete the registration
			await _firebaseClient
				.Child("GymRegistrations")
				.Child(id)
				.DeleteAsync();

			return NoContent();
		}
	}
}
