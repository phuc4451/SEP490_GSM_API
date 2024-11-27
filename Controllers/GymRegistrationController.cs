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
using Alpha_API.ViewModel;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class GymRegistrationController : ControllerBase
	{
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly RegisterService _registerService;
		private readonly QrCodeService _qrCodeService;
		private readonly GymMembershipCheckService _gymMembershipCheckService;

		private readonly FirebaseClientProvider _firebaseClientProvider;

		public GymRegistrationController(RegisterService registerService, FirebaseClientProvider firebaseClientProvider, FirebaseClient firebaseClient
			, FirebaseAuth firebaseAuth, QrCodeService qrCodeService, GymMembershipCheckService gymMembershipCheckService)
		{
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_registerService = registerService;
			_firebaseClientProvider = firebaseClientProvider;
			_qrCodeService = qrCodeService;
			_gymMembershipCheckService=gymMembershipCheckService;
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

			foreach (var registration in registrations)
			{
				registration.Object.RegistrationId = registration.Key;
			}

			return registrations.Select(r => r.Object).ToList();
		}

		// GET: api/GymRegistration/GetRegisters
		[HttpGet("GetRegisters")]
		[Authorize(Roles = "admin,staff")]
		public async Task<ActionResult<IEnumerable<GymRegisterDto>>> GetRegisters()
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var registrations = await _firebaseClient
				.Child("GymRegistrations")
				.OnceAsync<GymRegistration>();

			foreach (var registration in registrations)
			{
				registration.Object.RegistrationId = registration.Key;
			}

			List<GymRegisterDto> registers = new List<GymRegisterDto>();

			foreach (var registration in registrations)
			{
				var user = await _firebaseClient
					.Child("users")
					.Child(registration.Object.UserId)
					.OnceSingleAsync<User>();

				var payment = await _firebaseClient
					.Child("Payments")
					.Child(registration.Object.PaymentId)
					.OnceSingleAsync<Payment>();

				GymRegisterDto register = new GymRegisterDto()
				{
					UserId = registration.Object.UserId,
					EndDate = registration.Object.EndDate,
					StartDate = registration.Object.StartDate,
					SessionLeft = registration.Object.SessionLeft,
					IsActive = registration.Object.IsActive,
					UserName = user.Name,
					PaymentStatus = payment.PaymentStatus,
					GymMembershipId = registration.Object.GymMembershipId,
				};

				if (register.PaymentStatus == "Completed" && register.IsActive)
					registers.Add(register);
			}

			return registers.ToList();
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
			registration.RegistrationId = id;

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

		// POST: api/GymRegistration/CheckRegistration/{id}
		[HttpPost("CheckRegistration/{id}")]
		[Authorize(Roles = "admin,staff,customer")]
		public async Task<ActionResult<bool>> CheckRegistration(string id)
		{
			var result = await _gymMembershipCheckService.CheckGymMembership(id);

			return Ok(result);
		}

		// POST: api/GymRegistration
		[HttpPost]
		//[Authorize(Roles = "admin,staff,customer")] //customer can only pay qr =>call qrcontroller
		public async Task<ActionResult<GymRegistration>> CreateRegistration(RegisterPackageRequest request)
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

			try
			{
				if (request.QRPayment)
				{
					var qrList = await _qrCodeService.GenerateQrCodeAsync(request);
					return Ok(qrList);
				}
				else if (!request.QRPayment)
				{
					await _registerService.RegisterGym(request, request.QRPayment);
					return Ok();
				}
				else { return BadRequest("QR Payment is required but was not provided."); }
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
