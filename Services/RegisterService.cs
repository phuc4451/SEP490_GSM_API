using Alpha_API.Models;
using Alpha_API.Utils;
using Firebase.Database;
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
using Microsoft.Extensions.Options;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Alpha_API.Services
{
	public class RegisterService
	{
		private readonly PaymentMethodService _paymentMethodService;
		private FirebaseClient _firebaseClient;
		private readonly EmailService _emailService;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly RoleService _roleService;

		public RegisterService(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider,
			PaymentMethodService paymentMethodService, EmailService emailService, RoleService roleService)
		{

			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			_paymentMethodService = paymentMethodService;
			_emailService = emailService;
			_roleService = roleService;
		}
		public async Task<RegisterResult> RegisterGym(RegisterRequest request, bool qrPayment)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var membership = await _firebaseClient
			.Child("GymMemberships")
			.Child(request.GymMembershipId)
			.OnceSingleAsync<GymMembership>();

			var userId = await _emailService.GetUserIdByEmail(request.Emails.FirstOrDefault());

			if (userId == null)
			{
				throw new InvalidOperationException($"No user found with the email {request.Emails.FirstOrDefault()}. The user must be registered.");
			}
			var roleName = await _roleService.GetRoleOfUser(userId);
			if (!roleName.Equals("customer"))
			{
				throw new UnauthorizedAccessException("User does not have the required 'customer' role.");
			}

			// Query to find all GymRegistrations with the specified UserId
			var existingRegistrations = await _firebaseClient
				.Child("GymRegistrations")
				.OrderBy("userId")
				.EqualTo(userId)
				.OnceAsync<GymRegistration>();

			if (existingRegistrations.Any())
			{
				// There are existing registrations for this user
				// Handle accordingly, such as returning a message or modifying logic
				foreach (var exReg in existingRegistrations)
				{
					if (exReg.Object.EndDate >= DateTime.Now || exReg.Object.SessionLeft > 0)
					{
						throw new InvalidOperationException("The user already has an active registration.");
					}

				}
			}

			GymRegistration gymRegistration = new GymRegistration()
			{
				UserId = userId,
				GymMembershipId = request.GymMembershipId,
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddMonths((int)membership.DurationMonths),
				SessionLeft = membership.SessionCount ?? 0,
				IsActive = false,
				PaymentId = "Pending",
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var gym = JsonSerializer.Serialize(gymRegistration, options);

			var registration = await _firebaseClient
				.Child("GymRegistrations")
				.PostAsync(gym);

			var methods = await _paymentMethodService.GetAllPaymentMethods();
			string paymentMethod = qrPayment ? "QR" : "cash";

			qrPayment = true;

			string qrPaymentMethodId = methods.FirstOrDefault(method => method.MethodName == paymentMethod)?.PaymentMethodId;

			Payment payment = new Payment()
			{
				GymRegistrationId = registration.Key,
				Amount = membership.Price,
				BoxingRegistrationId = "",
				TrainerRentalRegistrationId = "",
				PaymentDate = DateTime.MinValue,
				PaymentMethodId = qrPaymentMethodId,
				PaymentStatus = "Pending",
				TransactionId = "Pending",
			};

			var paymentJSON = JsonSerializer.Serialize(payment, options);

			// Prepare the request data
			string info = "";

			if (qrPayment)
			{
				info = "CK" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}
			else
			{
				info = "TM" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}

			await _firebaseClient
				.Child("Payments")
				.Child(info)
				.PutAsync(paymentJSON);

			payment.PaymentId = info;

			gymRegistration.PaymentId = payment.PaymentId;

			gym = JsonSerializer.Serialize(gymRegistration, options);

			await _firebaseClient
				.Child("GymRegistrations")
				.Child(registration.Key)
				.PatchAsync(gym);

			return new RegisterResult
			{
				Membership = membership,
				Registration = registration,
				Payment = payment,
				Info = info

			};
			//return (membership, registration, payment, info);
		}

		public async Task<RegisterResult> RegisterTrainerRental(RegisterRequest request,
			bool qrPayment)
		{
			if (string.IsNullOrEmpty(request.ScheduleId))
			{
				throw new ArgumentException("Missing Schedule", nameof(request.ScheduleId));
			}

			var plan = await _firebaseClient
							.Child("TrainerRentalPlans")
							.Child(request.TrainerRentalPlanId)
							.OnceSingleAsync<TrainerRentalPlan>();

			var option = await _firebaseClient
							.Child("RentalOptions")
							.Child(plan.RentalOptionId)
							.OnceSingleAsync<RentalOption>();
			StringBuilder userIds = new StringBuilder();

			// Get all TrainerRentalRegistrations for this user
			var existingRegistrations = await _firebaseClient
				.Child("TrainerRentalRegistrations")
				.OnceAsync<TrainerRentalRegistration>();

			// Check if there are any registrations that contain the target userId and are still active
			bool hasActiveRegistration = false;

			foreach (var email in request.Emails)
			{
				var userId = await _emailService.GetUserIdByEmail(email);
				hasActiveRegistration = existingRegistrations.Any(reg =>
				reg.Object.UserIds != null &&
				reg.Object.UserIds.Split(',').Contains(userId) &&
				reg.Object.EndDate >= DateTime.Now
				);
				if (userId == null)
				{
					throw new InvalidOperationException($"No user found with the email {email}. The user must be registered.");
				}
				var roleName = await _roleService.GetRoleOfUser(userId);
				if (!roleName.Equals("customer"))
				{
					throw new UnauthorizedAccessException("User does not have the required 'customer' role.");
				}
				if (hasActiveRegistration)
				{
					throw new InvalidOperationException("The user already has an active Trainer Rental Registration.");
				}

				userIds.Append(userId).Append(",");
			}

			// Continue with the registration process

			if (userIds.Length > 0)
			{
				userIds.Length--; // This removes the last comma
			}

			string userIdsString = userIds.ToString();

			//// Query to find all GymRegistrations with the specified UserId
			//var existingRegistrations = await _firebaseClient
			//	.Child("TrainerRentalRegistrations")
			//	.OrderBy("userId")
			//	.EqualTo(userIdsString)
			//	.OnceAsync<TrainerRentalRegistration>();


			//if (existingRegistrations.Any())
			//{
			//	// There are existing registrations for this user
			//	// Handle accordingly, such as returning a message or modifying logic
			//	foreach (var exReg in existingRegistrations)
			//	{
			//		if (exReg.Object.EndDate >= DateTime.Now || exReg.Object.SessionLeft > 0)
			//		{
			//			throw new InvalidOperationException("The user already has an active registration.");
			//		}

			//	}
			//}

			if (!request.DurationMonths.HasValue && !request.Sessions.HasValue || request.Sessions == 0 && request.DurationMonths == 0)
			{
				throw new ArgumentException("Invalid months or sessions");
			}
			TrainerRentalRegistration trainerRegistration = new TrainerRentalRegistration()
			{
				UserIds = userIdsString,
				PlanId = request.TrainerRentalPlanId,
				ScheduleId = request.ScheduleId,
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddMonths(request.DurationMonths ?? 0),
				SessionLeft = request.Sessions ?? 0,
				IsActive = false,
				PaymentId = "Pending"
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var trainerRental = JsonSerializer.Serialize(trainerRegistration, options);

			var registration = await _firebaseClient
							.Child("TrainerRentalRegistrations")
							.PostAsync(trainerRental);

			var methods = await _paymentMethodService.GetAllPaymentMethods();
			string paymentMethod = qrPayment ? "QR" : "cash";
			string qrPaymentMethodId = methods.FirstOrDefault(method => method.MethodName == paymentMethod)?.PaymentMethodId;

			decimal price = 0;
			if (request.Sessions != null && request.Sessions >= option.SessionCountMin && request.Sessions <= option.SessionCountMax && request.Emails.Count == option.MemberCount)
			{
				price = (decimal)(option.PricePerPersonPerSession * request.Sessions * request.Emails.Count);
			}
			else if (request.DurationMonths != null && request.Emails.Count == option.MemberCount)
			{
				price = (decimal)(option.PricePerPersonPerMonth * request.DurationMonths * request.Emails.Count);
			}
			else
			{
				throw new ArgumentException("Fill missing required fields in trainer rental register form");
			}

			Payment payment = new Payment()
			{
				TrainerRentalRegistrationId = registration.Key,
				Amount = price,
				GymRegistrationId = "",
				BoxingRegistrationId = "",
				PaymentDate = DateTime.MinValue,
				PaymentMethodId = qrPaymentMethodId,
				PaymentStatus = "Pending",
				TransactionId = "Pending",
			};

			var paymentJSON = JsonSerializer.Serialize(payment, options);

			// Prepare the request data
			string info = "";

			if (qrPayment)
			{
				info = "CK" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}
			else
			{
				info = "TM" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}

			await _firebaseClient
				.Child("Payments")
				.Child(info)
				.PutAsync(paymentJSON);

			payment.PaymentId = info;

			trainerRegistration.PaymentId = payment.PaymentId;

			trainerRental = JsonSerializer.Serialize(trainerRegistration, options);

			await _firebaseClient
							.Child("TrainerRentalRegistrations")
							.Child(registration.Key)
							.PatchAsync(trainerRental);
			return new RegisterResult
			{
				RentalPlan = plan,
				RentalOption = option,
				Registration = registration,
				Payment = payment,
				Info = info

			};
			//return (plan, option, registration, payment, info);
		}
		public async Task<RegisterResult> RegisterBoxing(RegisterRequest request,
			bool qrPayment)
		{
			if (string.IsNullOrEmpty(request.ScheduleId))
			{
				throw new ArgumentException("Missing Schedule", nameof(request.ScheduleId));
			}

			var plan = await _firebaseClient
							.Child("BoxingMembershipPlans")
							.Child(request.BoxingMembershipPlanId)
							.OnceSingleAsync<BoxingMembershipPlan>();

			var option = await _firebaseClient
							.Child("BoxingOptions")
							.Child(plan.BoxingOptionId)
							.OnceSingleAsync<BoxingOption>();
			StringBuilder userIds = new StringBuilder();
			// Get all TrainerRentalRegistrations for this user
			var existingRegistrations = await _firebaseClient
				.Child("BoxingRegistrations")
				.OnceAsync<BoxingRegistration>();

			// Check if there are any registrations that contain the target userId and are still active
			bool hasActiveRegistration = false;

			foreach (var email in request.Emails)
			{
				var userId = await _emailService.GetUserIdByEmail(email);
				hasActiveRegistration = existingRegistrations.Any(reg =>
				reg.Object.UserIds != null &&
				reg.Object.UserIds.Split(',').Contains(userId) &&
				reg.Object.EndDate >= DateTime.Now
				);
				if (userId == null)
				{
					throw new InvalidOperationException($"No user found with the email {email}. The user must be registered.");
				}
				var roleName = await _roleService.GetRoleOfUser(userId);
				if (!roleName.Equals("customer"))
				{
					throw new UnauthorizedAccessException("User does not have the required 'customer' role.");
				}
				if (hasActiveRegistration)
				{
					throw new InvalidOperationException("The user already has an active Trainer Rental Registration.");
				}
				userIds.Append(userId).Append(",");
			}

			// Continue with the registration process

			if (userIds.Length > 0)
			{
				userIds.Length--; // This removes the last comma
			}

			string userIdsString = userIds.ToString();

			//// Query to find all GymRegistrations with the specified UserId
			//var existingRegistrations = await _firebaseClient
			//	.Child("BoxingRegistrations")
			//	.OrderBy("userId")
			//	.EqualTo(userIdsString)
			//	.OnceAsync<BoxingRegistration>();

			//if (existingRegistrations.Any())
			//{
			//	// There are existing registrations for this user
			//	// Handle accordingly, such as returning a message or modifying logic
			//	foreach (var exReg in existingRegistrations)
			//	{
			//		if (exReg.Object.EndDate >= DateTime.Now || exReg.Object.SessionLeft > 0)
			//		{
			//			throw new InvalidOperationException("The user already has an active registration.");
			//		}

			//	}
			//}

			BoxingRegistration boxingRegistration = new BoxingRegistration()
			{
				UserIds = userIdsString,
				ScheduleId = request.ScheduleId,
				BoxingMembershipPlanId = request.BoxingMembershipPlanId,
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddMonths(option.Months),
				SessionLeft = option.Sessions,
				IsActive = false,
				PaymentId = "Pending"
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var boxingReg = JsonSerializer.Serialize(boxingRegistration, options);

			var registration = await _firebaseClient
							.Child("BoxingRegistrations")
							.PostAsync(boxingReg);

			var methods = await _paymentMethodService.GetAllPaymentMethods();
			string paymentMethod = qrPayment ? "QR" : "cash";
			string qrPaymentMethodId = methods.FirstOrDefault(method => method.MethodName == paymentMethod)?.PaymentMethodId;

			decimal price = 0;
			if (request.Emails.Count == option.MemberCount)
			{
				price = option.TotalPrice;
			}
			else
			{
				throw new ArgumentException("Number of students does not match the package", nameof(request.Emails.Count));
			}

			Payment payment = new Payment()
			{
				BoxingRegistrationId = registration.Key,
				Amount = price,
				TrainerRentalRegistrationId = "",
				GymRegistrationId = "",
				PaymentDate = DateTime.MinValue,
				PaymentMethodId = qrPaymentMethodId,
				PaymentStatus = "Pending",
				TransactionId = "Pending",
			};

			var paymentJSON = JsonSerializer.Serialize(payment, options);

			// Prepare the request data
			string info = "";

			if (qrPayment)
			{
				info = "CK" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}
			else
			{
				info = "TM" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}

			await _firebaseClient
				.Child("Payments")
				.Child(info)
				.PutAsync(paymentJSON);

			payment.PaymentId = info;

			boxingRegistration.PaymentId = payment.PaymentId;

			boxingReg = JsonSerializer.Serialize(boxingRegistration, options);

			await _firebaseClient
							.Child("BoxingRegistrations")
							.Child(registration.Key)
							.PatchAsync(boxingReg);

			return new RegisterResult
			{
				BoxingPlan = plan,
				BoxingOption = option,
				Registration = registration,
				Payment = payment,
				Info = info

			};

			//return (plan, option, registration, payment, info);
		}
	}
}
