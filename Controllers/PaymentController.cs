using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Alpha_API.Controllers
{
	[ApiController]
	[Route("api/payment")]
	public class PaymentController : ControllerBase
	{
		private readonly ILogger<PaymentController> _logger;
		private readonly FirebaseAuth _firebaseAuth;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public PaymentController(ILogger<PaymentController> logger, FirebaseClient firebaseClient, FirebaseAuth firebaseAuth, FirebaseClientProvider firebaseClientProvider)
		{
			_logger = logger;
			_firebaseAuth = firebaseAuth;
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		[HttpPost("webhook")]
		public async Task<IActionResult> HandlePaymentWebhook([FromBody] PaymentWebhookModel paymentData)
		{
			try
			{
				// Log received webhook data
				_logger.LogInformation($"Received Payment Webhook: {JsonConvert.SerializeObject(paymentData)}");

				// Validate required fields
				if (string.IsNullOrEmpty(paymentData.Code) ||
					string.IsNullOrEmpty(paymentData.Content) ||
					paymentData.TransferAmount <= 0 ||
					string.IsNullOrEmpty(paymentData.TransactionDate))
				{
					return BadRequest(new { success = false, message = "Invalid payload, missing required fields." });
				}

				// Process the payment (dummy processing logic for now)
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				var trans = new
				{
					TransactionDate = DateTime.ParseExact(paymentData.TransactionDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
					TransferAmount = paymentData.TransferAmount ?? 0,
					AccountNumber = paymentData.AccountNumber,
					Code = paymentData.Code,
					Content = paymentData.Content,
					Gateway = paymentData.Gateway,
					TransferType = paymentData.TransferType,
				};

				var options = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
				};

				var jsonString = System.Text.Json.JsonSerializer.Serialize(trans, options);

				await _firebaseClient
					.Child("Transactions")
					.Child(paymentData.Id.ToString())
					.PutAsync(jsonString);

				await _firebaseClient
					.Child("Payments")
					.Child(trans.Code)
					.PatchAsync(new
					{
						transactionId = paymentData.Id.ToString(),
						paymentStatus = "Completed",
						paymentDate = trans.TransactionDate
					});

				var payment = await _firebaseClient
					.Child("Payments")
					.Child(trans.Code)
					.OnceSingleAsync<Payment>();

				if (!string.IsNullOrEmpty(payment.GymRegistrationId))
				{
					await _firebaseClient
						.Child("GymRegistrations")
						.Child(payment.GymRegistrationId)
						.PatchAsync(new
						{
							isActive = true,
						});
				}

				if (!string.IsNullOrEmpty(payment.TrainerRentalRegistrationId))
				{
					await _firebaseClient
						.Child("TrainerRentalRegistrations")
						.Child(payment.TrainerRentalRegistrationId)
						.PatchAsync(new
						{
							isActive = true,
						});
				}

				if (!string.IsNullOrEmpty(payment.BoxingRegistrationId))
				{
					await _firebaseClient
						.Child("BoxingRegistrations")
						.Child(payment.BoxingRegistrationId)
						.PatchAsync(new
						{
							isActive = true,
						});
				}

				// Return a success response
				return StatusCode(201, new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error processing payment webhook: {ex.Message}");
				return StatusCode(500, new { success = false, message = "Internal server error." });
			}
		}

		public class PaymentWebhookModel
		{
			public string? Gateway { get; set; }
			public string? TransactionDate { get; set; }
			public string? AccountNumber { get; set; }
			public string? SubAccount { get; set; }
			public string? Code { get; set; }
			public string? Content { get; set; }
			public string? TransferType { get; set; }
			public string? Description { get; set; }
			public decimal? TransferAmount { get; set; }
			public string? ReferenceCode { get; set; }
			public int? Accumulated { get; set; }
			public int? Id { get; set; }
		}


	}
}
