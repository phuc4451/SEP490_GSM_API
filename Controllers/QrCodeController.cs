using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Alpha_API.Utils;

namespace Alpha_API.Controllers
{
	[ApiController]
	[Route("api/qr")]
	public class QrCodeController : ControllerBase
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly PaymentMethodService _paymentMethodService;
		private FirebaseClient _firebaseClient;
		private readonly EmailService _emailService;
		private readonly RegisterService _registerService;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private string _createdPaymentId; // To keep track of created memberships
		private string _createdRegistrationId; // To keep track of created memberships
		private System.Timers.Timer _deleteTimer; // Timer for deletion

		public QrCodeController(IHttpClientFactory httpClientFactory, FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider,
			PaymentMethodService paymentMethodService, EmailService emailService, RegisterService registerService)
		{
			_httpClientFactory = httpClientFactory;
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			_paymentMethodService = paymentMethodService;
			_emailService = emailService;
			_registerService = registerService;
		}

		[HttpPost("generate")]
		public async Task<IActionResult> GenerateQrCode([FromBody] RegisterRequest request)
		{
			if (request == null)
			{
				return BadRequest("Request cannot be null.");
			}

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var client = _httpClientFactory.CreateClient();
			var apiUrl = "https://api.vietqr.io/v2/generate";

			//// Prepare the request data
			//var info = "DK" + Guid.NewGuid().ToString();

			bool qrPayment = true;

			// Add headers
			client.DefaultRequestHeaders.Add("x-client-id", "e27b68d6-aadc-44b8-bee3-9b77f39e9e0e");
			client.DefaultRequestHeaders.Add("x-api-key", "3163d46b-a727-4cc8-a841-fd8e0910dd57");
			var qrList = new List<object>(); // List to hold both qrDataUrl and course details


			//var courses = await _firebaseClient.Child("Courses").OnceAsync<Course>();

			//var courseList = new List<Course>();
			//foreach (var course in courses)
			//{
			//	course.Object.CourseId = course.Key;
			//	courseList.Add(course.Object);
			//}
			if (!string.IsNullOrEmpty(request.GymMembershipId))
			{
				try
				{
					//var membership = await _firebaseClient
					//.Child("GymMemberships")
					//.Child(request.GymMembershipId)
					//.OnceSingleAsync<GymMembership>();

					//var userId = await _emailService.GetUserIdByEmail(request.Emails.FirstOrDefault());

					//GymRegistration gymRegistration = new GymRegistration()
					//{
					//	UserId = userId,
					//	GymMembershipId = request.GymMembershipId,
					//	StartDate = DateTime.Now,
					//	EndDate = DateTime.Now.AddMonths((int)membership.DurationMonths),
					//	SessionLeft = membership.SessionCount ?? 0,
					//	IsActive = false,
					//	PaymentId = "Pending",
					//};

					//var gym = JsonSerializer.Serialize(gymRegistration, options);

					//var registration = await _firebaseClient
					//	.Child("GymRegistrations")
					//	.PostAsync(gym);

					//var methods = await _paymentMethodService.GetAllPaymentMethods();
					//string paymentMethod = qrPayment ? "QR" : "cash";
					//string qrPaymentMethodId = methods.FirstOrDefault(method => method.MethodName == paymentMethod)?.PaymentMethodId;

					//Payment payment = new Payment()
					//{
					//	GymRegistrationId = registration.Key,
					//	Amount = membership.Price,
					//	BoxingRegistrationId = "",
					//	TrainerRentalRegistrationId = "",
					//	PaymentDate = DateTime.MinValue,
					//	PaymentMethodId = qrPaymentMethodId,
					//	PaymentStatus = "Pending",
					//	TransactionId = "Pending",
					//};

					//var paymentJSON = JsonSerializer.Serialize(payment, options);

					//await _firebaseClient
					//	.Child("Payments")
					//	.Child(info)
					//	.PutAsync(paymentJSON);

					//payment.PaymentId = info;

					//gymRegistration.PaymentId = payment.PaymentId;

					//gym = JsonSerializer.Serialize(gymRegistration, options);

					//await _firebaseClient
					//	.Child("GymRegistrations")
					//	.Child(registration.Key)
					//	.PatchAsync(gym);
					var registerService = await _registerService.RegisterGym(request, qrPayment);
					var membership = registerService.Membership;
					var registration = registerService.Registration;
					var payment = registerService.Payment;
					var info = registerService.info;

					//// Save membership to Firebase
					//var membershipId = info; // Save membership ID for deletion later
					_createdPaymentId = payment.PaymentId; // Store the created membership ID
					_createdRegistrationId = registration.Key; // Store the created membership ID

					var jsonData = new
					{
						accountNo = "0978788128",
						accountName = "DINH DAI DUONG",
						acqId = "970422",
						addInfo = info,
						amount = payment.Amount,
						template = "compact"
					};

					var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);
					var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

					// Send POST request to VietQR API
					HttpResponseMessage response = await client.PostAsync(apiUrl, content);

					if (response.IsSuccessStatusCode)
					{
						var responseData = await response.Content.ReadAsStringAsync();
						var jsonResponse = JObject.Parse(responseData);

						var qrDataUrl = jsonResponse["data"]?["qrDataURL"]?.ToString();

						if (!string.IsNullOrEmpty(qrDataUrl))
						{
							qrList.Add(new
							{
								qrDataUrl,
								gymMembership = new
								{
									membership.Name,
									membership.DurationMonths,
									membership.SessionCount,
									TotalPrice = membership.Price,
								}
							});
						}
						else
						{
							return BadRequest("QR data URL not found in response.");
						}
					}
					else
					{
						return StatusCode((int)response.StatusCode, $"Error: {response.ReasonPhrase}");
					}
				}
				catch (InvalidOperationException ex)
				{
					// Handle the specific exception
					return BadRequest(ex.Message);
				}
				catch (ArgumentException ex)
				{
					// Handle specific ArgumentExceptions as BadRequest
					return BadRequest(ex.Message);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}

			if (!string.IsNullOrEmpty(request.TrainerRentalPlanId))
			{
				try
				{
					//if (string.IsNullOrEmpty(request.ScheduleId))
					//{
					//	return BadRequest("Missing Schedule");
					//}
					//var plan = await _firebaseClient
					//	.Child("TrainerRentalPlans")
					//	.Child(request.TrainerRentalPlanId)
					//	.OnceSingleAsync<TrainerRentalPlan>();

					//var option = await _firebaseClient
					//	.Child("RentalOptions")
					//	.Child(plan.RentalOptionId)
					//	.OnceSingleAsync<RentalOption>();
					//StringBuilder userIds = new StringBuilder();

					//foreach (var email in request.Emails)
					//{
					//	var userId = await _emailService.GetUserIdByEmail(email);
					//	userIds.Append(userId).Append(",");
					//}

					//if (userIds.Length > 0)
					//{
					//	userIds.Length--; // This removes the last comma
					//}

					//string userIdsString = userIds.ToString();

					//if (!request.DurationMonths.HasValue && !request.Sessions.HasValue || request.Sessions == 0 && request.DurationMonths == 0)
					//{
					//	return BadRequest("Invalid months or sessions");
					//}
					//TrainerRentalRegistration trainerRegistration = new TrainerRentalRegistration()
					//{
					//	UserIds = userIdsString,
					//	PlanId = request.TrainerRentalPlanId,
					//	ScheduleId = request.ScheduleId,
					//	StartDate = DateTime.Now,
					//	EndDate = DateTime.Now.AddMonths(request.DurationMonths ?? 0),
					//	SessionLeft = request.Sessions ?? 0,
					//	IsActive = false,
					//	PaymentId = "Pending"
					//};

					//var trainerRental = JsonSerializer.Serialize(trainerRegistration, options);

					//var registration = await _firebaseClient
					//	.Child("TrainerRentalRegistrations")
					//	.PostAsync(trainerRental);

					//var methods = await _paymentMethodService.GetAllPaymentMethods();
					//string paymentMethod = qrPayment ? "QR" : "cash";
					//string qrPaymentMethodId = methods.FirstOrDefault(method => method.MethodName == paymentMethod)?.PaymentMethodId;

					//decimal price = 0;
					//if (request.Sessions != null && request.Sessions >= option.SessionCountMin && request.Sessions <= option.SessionCountMax && request.Emails.Count == option.MemberCount)
					//{
					//	price = (decimal)(option.PricePerPersonPerSession * request.Sessions * request.Emails.Count);
					//}
					//else if (request.DurationMonths != null && request.Emails.Count == option.MemberCount)
					//{
					//	price = (decimal)(option.PricePerPersonPerMonth * request.DurationMonths * request.Emails.Count);
					//}
					//else
					//{
					//	return BadRequest("Fill missing required fields in trainer rental register form");
					//}

					//Payment payment = new Payment()
					//{
					//	TrainerRentalRegistrationId = registration.Key,
					//	Amount = price,
					//	GymRegistrationId = "",
					//	BoxingRegistrationId = "",
					//	PaymentDate = DateTime.MinValue,
					//	PaymentMethodId = qrPaymentMethodId,
					//	PaymentStatus = "Pending",
					//	TransactionId = "Pending",
					//};

					//var paymentJSON = JsonSerializer.Serialize(payment, options);

					//await _firebaseClient
					//	.Child("Payments")
					//	.Child(info)
					//	.PutAsync(paymentJSON);

					//payment.PaymentId = info;

					//trainerRegistration.PaymentId = payment.PaymentId;

					//trainerRental = JsonSerializer.Serialize(trainerRegistration, options);

					//await _firebaseClient
					//	.Child("TrainerRentalRegistrations")
					//	.Child(registration.Key)
					//	.PatchAsync(trainerRental);

					var registerService = await _registerService.RegisterTrainerRental(request, qrPayment);
					var plan = registerService.Plan;
					var option = registerService.Option;
					var registration = registerService.Registration;
					var payment = registerService.Payment;
					var info = registerService.info;
					//// Save membership to Firebase
					//var membershipId = info; // Save membership ID for deletion later
					_createdPaymentId = payment.PaymentId; // Store the created membership ID
					_createdRegistrationId = registration.Key; // Store the created membership ID

					var jsonData = new
					{
						accountNo = "0978788128",
						accountName = "DINH DAI DUONG",
						acqId = "970422",
						addInfo = info,
						amount = payment.Amount,
						template = "compact"
					};

					var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);
					var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

					// Send POST request to VietQR API
					HttpResponseMessage response = await client.PostAsync(apiUrl, content);

					if (response.IsSuccessStatusCode)
					{
						var responseData = await response.Content.ReadAsStringAsync();
						var jsonResponse = JObject.Parse(responseData);

						var qrDataUrl = jsonResponse["data"]?["qrDataURL"]?.ToString();

						if (!string.IsNullOrEmpty(qrDataUrl))
						{
							qrList.Add(new
							{
								qrDataUrl,
								trainerRentalPlan = new
								{
									plan.TrainerId,
									option.Description,
									option.PricePerPersonPerSession,
									option.PricePerPersonPerMonth,
									option.MemberCount,
									TotalPrice = payment.Amount
								}
							});
						}
						else
						{
							return BadRequest("QR data URL not found in response.");
						}
					}
					else
					{
						return StatusCode((int)response.StatusCode, $"Error: {response.ReasonPhrase}");
					}
				}
				catch (InvalidOperationException ex)
				{
					// Handle the specific exception
					return BadRequest(ex.Message);
				}
				catch (ArgumentException ex)
				{
					// Handle specific ArgumentExceptions as BadRequest
					return BadRequest(ex.Message);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}

			if (!string.IsNullOrEmpty(request.BoxingMembershipPlanId))
			{
				try
				{
					//if (string.IsNullOrEmpty(request.ScheduleId))
					//{
					//	return BadRequest("Missing Schedule");
					//}

					//var plan = await _firebaseClient
					//	.Child("BoxingMembershipPlans")
					//	.Child(request.BoxingMembershipPlanId)
					//	.OnceSingleAsync<BoxingMembershipPlan>();

					//var option = await _firebaseClient
					//	.Child("BoxingOptions")
					//	.Child(plan.BoxingOptionId)
					//	.OnceSingleAsync<BoxingOption>();
					//StringBuilder userIds = new StringBuilder();

					//foreach (var email in request.Emails)
					//{
					//	var userId = await _emailService.GetUserIdByEmail(email);
					//	userIds.Append(userId).Append(",");
					//}

					//if (userIds.Length > 0)
					//{
					//	userIds.Length--; // This removes the last comma
					//}

					//string userIdsString = userIds.ToString();

					//BoxingRegistration boxingRegistration = new BoxingRegistration()
					//{
					//	UserIds = userIdsString,
					//	ScheduleId = request.ScheduleId,
					//	BoxingMembershipPlanId = request.BoxingMembershipPlanId,
					//	StartDate = DateTime.Now,
					//	EndDate = DateTime.Now.AddMonths(option.Months),
					//	SessionLeft = option.Sessions,
					//	IsActive = false,
					//	PaymentId = "Pending"
					//};

					//var boxingReg = JsonSerializer.Serialize(boxingRegistration, options);

					//var registration = await _firebaseClient
					//	.Child("BoxingRegistrations")
					//	.PostAsync(boxingReg);

					//var methods = await _paymentMethodService.GetAllPaymentMethods();
					//string paymentMethod = qrPayment ? "QR" : "cash";
					//string qrPaymentMethodId = methods.FirstOrDefault(method => method.MethodName == paymentMethod)?.PaymentMethodId;

					//decimal price = 0;
					//if (request.Emails.Count == option.MemberCount)
					//{
					//	price = option.TotalPrice;
					//}
					//else
					//{
					//	return BadRequest("Number of students does not match the package");
					//}

					//Payment payment = new Payment()
					//{
					//	BoxingRegistrationId = registration.Key,
					//	Amount = price,
					//	TrainerRentalRegistrationId = "",
					//	GymRegistrationId = "",
					//	PaymentDate = DateTime.MinValue,
					//	PaymentMethodId = qrPaymentMethodId,
					//	PaymentStatus = "Pending",
					//	TransactionId = "Pending",
					//};

					//var paymentJSON = JsonSerializer.Serialize(payment, options);

					//await _firebaseClient
					//	.Child("Payments")
					//	.Child(info)
					//	.PutAsync(paymentJSON);

					//payment.PaymentId = info;

					//boxingRegistration.PaymentId = payment.PaymentId;

					//boxingReg = JsonSerializer.Serialize(boxingRegistration, options);

					//await _firebaseClient
					//	.Child("BoxingRegistrations")
					//	.Child(registration.Key)
					//	.PatchAsync(boxingReg);

					var registerService = await _registerService.RegisterBoxing(request, qrPayment);
					var plan = registerService.Plan;
					var option = registerService.Option;
					var registration = registerService.Registration;
					var payment = registerService.Payment;
					var info = registerService.info;
					//// Save membership to Firebase
					//var membershipId = info; // Save membership ID for deletion later
					_createdPaymentId = payment.PaymentId; // Store the created membership ID
					_createdRegistrationId = registration.Key; // Store the created membership ID

					var jsonData = new
					{
						accountNo = "0978788128",
						accountName = "DINH DAI DUONG",
						acqId = "970422",
						addInfo = info,
						amount = payment.Amount,
						template = "compact"
					};

					var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);
					var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

					// Send POST request to VietQR API
					HttpResponseMessage response = await client.PostAsync(apiUrl, content);

					if (response.IsSuccessStatusCode)
					{
						var responseData = await response.Content.ReadAsStringAsync();
						var jsonResponse = JObject.Parse(responseData);

						var qrDataUrl = jsonResponse["data"]?["qrDataURL"]?.ToString();

						if (!string.IsNullOrEmpty(qrDataUrl))
						{
							qrList.Add(new
							{
								qrDataUrl,
								boxingMembershipPlan = new
								{
									plan.BoxingTrainerId,
									option.Description,
									option.TotalPrice,
									option.Sessions,
									option.MemberCount,
									option.Months,
								}
							});
						}
						else
						{
							return BadRequest("QR data URL not found in response.");
						}
					}
					else
					{
						return StatusCode((int)response.StatusCode, $"Error: {response.ReasonPhrase}");
					}
				}
				catch (InvalidOperationException ex)
				{
					// Handle the specific exception
					return BadRequest(ex.Message);
				}
				catch (ArgumentException ex)
				{
					// Handle specific ArgumentExceptions as BadRequest
					return BadRequest(ex.Message);
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}

			// Start the timer for deletion
			StartDeletionTimer();

			return Ok(new { qrList }); // Return the Data URI
		}

		private void StartDeletionTimer()
		{
			// If the timer is already running, stop it
			if (_deleteTimer != null)
			{
				_deleteTimer.Stop();
				_deleteTimer.Dispose();
			}

			// Create and start a new timer
			_deleteTimer = new System.Timers.Timer(10000); // Set for 10 seconds
			_deleteTimer.Elapsed += async (sender, e) => await DeleteMembershipsAsync();
			_deleteTimer.AutoReset = false; // Run only once
			_deleteTimer.Start();
		}

		private async Task DeleteMembershipsAsync()
		{

			//await _firebaseClient.Child("Memberships").Child(_createdPaymentId).DeleteAsync();

			_createdPaymentId = ""; // Clear the list after deletion
			_createdRegistrationId = "";
		}
	}
}
