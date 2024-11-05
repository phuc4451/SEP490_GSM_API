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
using Alpha_API.Utils;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BoxingRegistrationController : ControllerBase
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly RegisterService _registerService;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly EmailService _emailService;

		public BoxingRegistrationController(RegisterService registerService, FirebaseClientProvider firebaseClientProvider, FirebaseClient firebaseClient, FirebaseAuth firebaseAuth, EmailService emailService)
		{
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_registerService = registerService;
			_firebaseClientProvider = firebaseClientProvider;
			_emailService = emailService;
		}

		// GET: api/BoxingRegistration
		[HttpGet]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<IEnumerable<BoxingRegistration>>> GetRegistrations()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var registrations = await _firebaseClient
				.Child("BoxingRegistrations")
				.OnceAsync<BoxingRegistration>();

			foreach (var registration in registrations)
			{
				registration.Object.RegistrationId = registration.Key;
			}

			return registrations.Select(r => r.Object).ToList();
		}

		// GET: api/BoxingRegistration/{id}
		[HttpGet("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<BoxingRegistration>> GetRegistration(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var registration = await _firebaseClient
				.Child("BoxingRegistrations")
				.Child(id)
				.OnceSingleAsync<BoxingRegistration>();

			if (registration == null)
			{
				return NotFound();
			}
			registration.RegistrationId = id;

			return registration;
		}

		// GET: api/BoxingRegistration/{id}
		[HttpGet("MyRegistration")]
		[Authorize(Roles = "customer")]
		public async Task<ActionResult<BoxingRegistration>> MyRegistration()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Retrieve the uid claim
			var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

			if (userIdClaim == null)
			{
				return Unauthorized("User not authenticated.");
			}

			var userId = userIdClaim.Value; // This is the Firebase UID

			var existingRegistrations = await _firebaseClient
					.Child("BoxingRegistrations")
					.OnceAsync<BoxingRegistration>();

			var matchingRegistration = existingRegistrations
				.FirstOrDefault(reg =>
					reg.Object.UserIds != null &&
					reg.Object.UserIds.Split(',').Contains(userId) &&
					reg.Object.EndDate >= DateTime.Now &&
					reg.Object.IsActive
				);

			if (matchingRegistration == null)
			{
				return NotFound();
			}

			return matchingRegistration.Object;
		}

		// POST: api/BoxingRegistration
		[HttpPost]
		[Authorize(Roles = "admin,staff")] //customer can only pay qr =>call qrcontroller
		public async Task<ActionResult<BoxingRegistration>> CreateRegistration(RegisterRequest request)
		{
			if (request.QRPayment)
			{
				return BadRequest("Unsupport");
			}
			var registerService = await _registerService.RegisterBoxing(request, request.QRPayment);

			return CreatedAtAction(nameof(GetRegistration), new { id = registerService.Registration.Key }, registerService.Registration);
		}

		// POST: api/BoxingRegistration/{id}
		[HttpPatch("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> ToggleRegistration(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var existingRegistration = await _firebaseClient
	.Child("BoxingRegistrations")
	.Child(id)
	.OnceSingleAsync<BoxingRegistration>();

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
				.Child("BoxingRegistrations")
				.Child(id)
				.PatchAsync(new
				{
					isActive = isActive
				});

			return NoContent();
		}

		// PUT: api/BoxingRegistration/{id}
		[HttpPut("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> UpdateRegistration(string id, BoxingRegistration registration)
		{
			return BadRequest("Goi api nay la hong day");
			var existingRegistration = await _firebaseClient
				.Child("BoxingRegistrations")
				.Child(id)
				.OnceSingleAsync<BoxingRegistration>();

			if (existingRegistration == null)
			{
				return NotFound();
			}

			// Update the registration
			await _firebaseClient
				.Child("BoxingRegistrations")
				.Child(id)
				.PutAsync(registration);

			return NoContent();
		}

		// DELETE: api/BoxingRegistration/{id}
		[HttpDelete("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> DeleteRegistration(string id)
		{
			return BadRequest("Goi api nay la hong day");
			var existingRegistration = await _firebaseClient
				.Child("BoxingRegistrations")
				.Child(id)
				.OnceSingleAsync<BoxingRegistration>();

			if (existingRegistration == null)
			{
				return NotFound();
			}

			// Delete the registration
			await _firebaseClient
				.Child("BoxingRegistrations")
				.Child(id)
				.DeleteAsync();

			return NoContent();
		}
	}
}
