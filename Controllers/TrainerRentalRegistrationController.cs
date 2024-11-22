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
using Alpha_API.ViewModel;

namespace Alpha_API.Controllers
{
    [Route("api/[controller]")]
	[ApiController]
	public class TrainerRentalRegistrationController : ControllerBase
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly RegisterService _registerService;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly EmailService _emailService;
		private readonly QrCodeService _qrCodeService;

		public TrainerRentalRegistrationController(RegisterService registerService, FirebaseClientProvider firebaseClientProvider, FirebaseClient firebaseClient
			, FirebaseAuth firebaseAuth, EmailService emailService, QrCodeService qrCodeService)
		{
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_registerService = registerService;
			_firebaseClientProvider = firebaseClientProvider;
			_emailService = emailService;
			_qrCodeService = qrCodeService;
		}

		// GET: api/TrainerRentalRegistration
		[HttpGet]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<IEnumerable<TrainerRentalRegistration>>> GetRegistrations()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var registrations = await _firebaseClient
				.Child("TrainerRentalRegistrations")
				.OnceAsync<TrainerRentalRegistration>();

			foreach (var registration in registrations)
			{
				registration.Object.RegistrationId = registration.Key;
			}

			return registrations.Select(r => r.Object).ToList();
		}

		// GET: api/TrainerRentalRegistration/{id}
		[HttpGet("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<TrainerRentalRegistration>> GetRegistration(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var registration = await _firebaseClient
				.Child("TrainerRentalRegistrations")
				.Child(id)
				.OnceSingleAsync<TrainerRentalRegistration>();

			if (registration == null)
			{
				return NotFound();
			}
			registration.RegistrationId = id;

			return registration;
		}

		// GET: api/TrainerRentalRegistration/{id}
		[HttpGet("MyRegistration")]
		[Authorize(Roles = "customer")]
		public async Task<ActionResult<TrainerRentalRegistration>> MyRegistration()
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
					.Child("TrainerRentalRegistrations")
					.OnceAsync<TrainerRentalRegistration>();

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

		// POST: api/TrainerRentalRegistration
		[HttpPost]
		//[Authorize(Roles = "admin,staff,customer")] //customer can only pay qr =>call qrcontroller
		public async Task<ActionResult<TrainerRentalRegistration>> CreateRegistration(RegisterPackageRequest request)
		{
			if (request == null)
			{
				return BadRequest("Request is null");
			}
			if (request.Emails.Count == 0)
			{
				return BadRequest("Emails are null");
			}
			if (string.IsNullOrEmpty(request.GymMembershipId) && string.IsNullOrEmpty(request.TrainerRentalPlanId) && string.IsNullOrEmpty(request.BoxingMembershipPlanId))
			{
				return BadRequest("Memberships or plans are null");
			}
			if (request.QRPayment)
			{
				try
				{
					var qrList = await _qrCodeService.GenerateQrCodeAsync(request);
					return Ok(qrList);
				}
				catch (ArgumentNullException ex)
				{
					return BadRequest(new { message = ex.Message });
				}
				catch (UnauthorizedAccessException ex)
				{
					return BadRequest(new { message = ex.Message });
				}
				catch (InvalidOperationException ex)
				{
					return BadRequest(new { message = ex.Message });
				}
				catch (ArgumentException ex)
				{
					// Handle specific ArgumentExceptions as BadRequest
					return BadRequest(ex.Message);
				}
				catch (HttpRequestException ex)
				{
					return StatusCode(502, new { message = "Failed to generate QR code", details = ex.Message });
				}
				catch (Exception ex)
				{
					// Log the exception (if using a logger)
					return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
				}
			}
			else if (!request.QRPayment)
			{
				var registerService = await _registerService.RegisterTrainerRental(request, request.QRPayment);
				return CreatedAtAction(nameof(GetRegistration), new { id = registerService.Registration.Key }, registerService.Registration);
			}
			else { return BadRequest("QR Payment is required but was not provided."); }
		}

		// POST: api/TrainerRentalRegistration/{id}
		[HttpPatch("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> ToggleRegistration(string id)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			var existingRegistration = await _firebaseClient
	.Child("TrainerRentalRegistrations")
	.Child(id)
	.OnceSingleAsync<TrainerRentalRegistration>();

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
				.Child("TrainerRentalRegistrations")
				.Child(id)
				.PatchAsync(new
				{
					isActive = isActive
				});

			return NoContent();
		}

		// PUT: api/TrainerRentalRegistration/{id}
		[HttpPut("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> UpdateRegistration(string id, TrainerRentalRegistration registration)
		{
			return BadRequest("Goi api nay la hong day");
			var existingRegistration = await _firebaseClient
				.Child("TrainerRentalRegistrations")
				.Child(id)
				.OnceSingleAsync<TrainerRentalRegistration>();

			if (existingRegistration == null)
			{
				return NotFound();
			}

			// Update the registration
			await _firebaseClient
				.Child("TrainerRentalRegistrations")
				.Child(id)
				.PutAsync(registration);

			return NoContent();
		}

		// DELETE: api/TrainerRentalRegistration/{id}
		[HttpDelete("{id}")]
		[Authorize(Roles = "admin,staff")]
		public async Task<IActionResult> DeleteRegistration(string id)
		{
			return BadRequest("Goi api nay la hong day");
			var existingRegistration = await _firebaseClient
				.Child("TrainerRentalRegistrations")
				.Child(id)
				.OnceSingleAsync<TrainerRentalRegistration>();

			if (existingRegistration == null)
			{
				return NotFound();
			}

			// Delete the registration
			await _firebaseClient
				.Child("TrainerRentalRegistrations")
				.Child(id)
				.DeleteAsync();

			return NoContent();
		}
	}
}
