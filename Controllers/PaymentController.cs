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
        [HttpDelete("deleteOrphanPayments")]
        public async Task<IActionResult> DeleteOrphanPayments()
        {
            try
            {
                // 1. Thu thập tất cả các PaymentId từ bảng Payments
                var allPayments = await _firebaseClient
                    .Child("Payments")
                    .OnceAsync<Payment>();

                var allPaymentIds = allPayments.Select(p => p.Key).ToHashSet();

                // 2. Thu thập tất cả các PaymentId được tham chiếu trong các bảng đăng ký
                var referencedPaymentIds = new HashSet<string>();

                // 2.1. BoxingRegistrations
                var boxingRegistrations = await _firebaseClient
                    .Child("BoxingRegistrations")
                    .OnceAsync<BoxingRegistration>();

                foreach (var reg in boxingRegistrations)
                {
                    if (!string.IsNullOrEmpty(reg.Object.PaymentId))
                    {
                        referencedPaymentIds.Add(reg.Object.PaymentId);
                    }
                }

                // 2.2. GymRegistrations
                var gymRegistrations = await _firebaseClient
                    .Child("GymRegistrations")
                    .OnceAsync<GymRegistration>();

                foreach (var reg in gymRegistrations)
                {
                    if (!string.IsNullOrEmpty(reg.Object.PaymentId))
                    {
                        referencedPaymentIds.Add(reg.Object.PaymentId);
                    }
                }

                // 2.3. TrainerRentalRegistrations
                var trainerRentalRegistrations = await _firebaseClient
                    .Child("TrainerRentalRegistrations")
                    .OnceAsync<TrainerRentalRegistration>();

                foreach (var reg in trainerRentalRegistrations)
                {
                    if (!string.IsNullOrEmpty(reg.Object.PaymentId))
                    {
                        referencedPaymentIds.Add(reg.Object.PaymentId);
                    }
                }

                // 3. Xác định các PaymentId không được tham chiếu (orphan)
                var orphanPaymentIds = allPaymentIds
                    .Where(pid => !referencedPaymentIds.Contains(pid))
                    .ToList();

                var deletedTransactionIds = new List<string>();

                // 4. Xóa các Payment "orphan" và các Transaction liên quan
                foreach (var orphanId in orphanPaymentIds)
                {
                    var payment = allPayments.FirstOrDefault(p => p.Key == orphanId);
                    if (payment != null)
                    {
                        var transactionId = payment.Object.TransactionId;

                        if (!string.IsNullOrEmpty(transactionId))
                        {
                            // Xóa Transaction
                            await _firebaseClient
                                .Child("Transactions")
                                .Child(transactionId)
                                .DeleteAsync();

                            deletedTransactionIds.Add(transactionId);
                        }

                        // Xóa Payment
                        await _firebaseClient
                            .Child("Payments")
                            .Child(orphanId)
                            .DeleteAsync();
                    }
                }

                return Ok(new
                {
                    Message = "Đã xóa thành công các Payment orphan cùng với các Transactions liên quan.",
                    DeletedPaymentIds = orphanPaymentIds,
                    DeletedTransactionIds = deletedTransactionIds
                });
            }
            catch (Exception)
            {
                // Trả về lỗi chung
                return StatusCode(500, "Đã xảy ra lỗi phía máy chủ.");
            }
        }
        [HttpGet("getByRegistrationId/{registrationId}")]
        public async Task<IActionResult> GetPaymentsByRegistrationId(string registrationId)
        {
            if (string.IsNullOrEmpty(registrationId))
            {
                return BadRequest("RegistrationId không được để trống.");
            }

            try
            {
                // Thực hiện ba truy vấn riêng biệt
                var boxingPaymentsTask = _firebaseClient
                    .Child("Payments")
                    .OrderBy("boxingRegistrationId")
                    .EqualTo(registrationId)
                    .OnceAsync<Payment>();

                var gymPaymentsTask = _firebaseClient
                    .Child("Payments")
                    .OrderBy("gymRegistrationId")
                    .EqualTo(registrationId)
                    .OnceAsync<Payment>();

                var trainerRentalPaymentsTask = _firebaseClient
                    .Child("Payments")
                    .OrderBy("trainerRentalRegistrationId")
                    .EqualTo(registrationId)
                    .OnceAsync<Payment>();

                await Task.WhenAll(boxingPaymentsTask, gymPaymentsTask, trainerRentalPaymentsTask);

                var boxingPayments = boxingPaymentsTask.Result;
                var gymPayments = gymPaymentsTask.Result;
                var trainerRentalPayments = trainerRentalPaymentsTask.Result;

                // Hợp nhất kết quả và loại bỏ các bản ghi trùng lặp
                var allPayments = boxingPayments
                    .Concat(gymPayments)
                    .Concat(trainerRentalPayments)
                    .GroupBy(p => p.Key)
                    .Select(g => new Payment
                    {
                        PaymentId = g.Key,
                        Amount = g.First().Object.Amount,
                        BoxingRegistrationId = g.First().Object.BoxingRegistrationId,
                        GymRegistrationId = g.First().Object.GymRegistrationId,
                        TrainerRentalRegistrationId = g.First().Object.TrainerRentalRegistrationId,
                        PaymentDate = g.First().Object.PaymentDate,
                        PaymentMethod = g.First().Object.PaymentMethod,
                        PaymentStatus = g.First().Object.PaymentStatus,
                        TransactionId = g.First().Object.TransactionId
                    })
                    .ToList();

                return Ok(allPayments);
            }
            catch (Exception ex)
            {
                // Trong môi trường phát triển, bạn có thể trả về chi tiết lỗi để dễ dàng debug
                // Trong môi trường sản xuất, hãy giữ nguyên phản hồi lỗi chung để bảo mật
                return StatusCode(500, $"Đã xảy ra lỗi phía máy chủ: {ex.Message}");
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
