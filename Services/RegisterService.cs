using Alpha_API.Models;
using Alpha_API.Utils;
using Firebase.Database;
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
using Alpha_API.ViewModel;
using System.Security.Claims;

namespace Alpha_API.Services
{
	public class RegisterService
	{
		private readonly PaymentMethodService _paymentMethodService;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseAuth _firebaseAuth;
		private readonly EmailService _emailService;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly RoleService _roleService;
		private readonly GymMembershipCheckService _gymMembershipCheckService;
		private readonly IScheduleService _scheduleService;
		private readonly Dictionary<string, System.Timers.Timer> _customerTimers = new();
		private readonly Dictionary<string, (string PaymentId, string RegistrationType, string RegistrationId, string ScheduleId)> _pendingRegistrations = new();
		private readonly double paymentWaitingTime = 100000;

		public RegisterService(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider,
			PaymentMethodService paymentMethodService, EmailService emailService, RoleService roleService,
			IScheduleService scheduleService, GymMembershipCheckService gymMembershipCheckService, FirebaseAuth firebaseAuth)
		{

			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			//_paymentMethodService = paymentMethodService;
			_emailService = emailService;
			_roleService = roleService;
			_scheduleService = scheduleService;
			_gymMembershipCheckService = gymMembershipCheckService;
			_firebaseAuth = firebaseAuth;
		}
		public async Task<RegisterResult> RegisterGym(RegisterPackageRequest request, bool qrPayment, string customerId)
		{
			//_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var user = await _firebaseAuth.GetUserByEmailAsync(request.Emails.FirstOrDefault());

			if (user == null)
			{
				throw new InvalidOperationException($"No user found with the email {request.Emails.FirstOrDefault()}. The user must be registered.");
			}

			if (!user.EmailVerified)
			{
				throw new InvalidOperationException($"Email {request.Emails.FirstOrDefault()} is not verified.");
			}

			var userId = user.Uid;
			var roleName = _roleService.GetRoleOfUser(userId);

			var membership = _firebaseClient
				.Child("GymMemberships")
				.Child(request.GymMembershipId)
				.OnceSingleAsync<GymMembership>();

			// Query to find all GymRegistrations with the specified UserId
			var existingRegistrations = _firebaseClient
				.Child("GymRegistrations")
				.OrderBy("userId")
				.EqualTo(userId)
				.OnceAsync<GymRegistration>();

			await Task.WhenAll(roleName, existingRegistrations, membership);

			if (!roleName.Result.Equals("customer"))
			{
				throw new UnauthorizedAccessException("User does not have the required 'customer' role.");
			}

			if (existingRegistrations.Result.Count != 0)
			{
				// Check for active registrations
				var hasActiveRegistration = existingRegistrations.Result.Any(exReg =>
				exReg.Value.IsActive &&
				(exReg.Value.EndDate >= DateTime.Now && exReg.Value.SessionLeft > 0));

				if (hasActiveRegistration)
				{
					throw new InvalidOperationException("The user already has an active registration.");
				}
			}

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};


			//var methods = await _paymentMethodService.GetAllPaymentMethods();
			string paymentMethod = qrPayment ? "QR" : "cash";

			//string qrPaymentMethodId = methods.FirstOrDefault(method => method.MethodName == paymentMethod)?.PaymentMethodId;

			var regisId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);

			string info = "";

			if (qrPayment)
			{
				info = "SEVQR" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}
			else
			{
				info = "TM" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}

			var expiryDate = DateTime.Now.AddMonths(membership.Result.DurationMonths ?? 0);
			expiryDate = new DateTime(expiryDate.Year, expiryDate.Month, expiryDate.Day, 23, 59, 59);


			GymRegistration gymRegistration = new GymRegistration()
			{
				UserId = userId,
				GymMembershipId = request.GymMembershipId,
				StartDate = DateTime.Now,
				EndDate = expiryDate,
				//SessionLeft = membership.Result.SessionCount ?? (DateTime.Now.AddMonths(membership.Result.DurationMonths ?? 0) - DateTime.Now).Days,
				SessionLeft = membership.Result.SessionCount ?? 999,
				IsActive = false,
				PaymentId = info,
			};

			var jsonString = JsonSerializer.Serialize(gymRegistration, options);

			var regTask = _firebaseClient
				.Child("GymRegistrations")
				.Child(regisId)
				.PutAsync(jsonString);

			Payment payment = new Payment()
			{
				GymRegistrationId = regisId,
				Amount = membership.Result.Price,
				BoxingRegistrationId = "",
				TrainerRentalRegistrationId = "",
				PaymentDate = DateTime.MinValue,
				PaymentMethod = paymentMethod,
				PaymentStatus = "Pending",
				TransactionId = "Pending",
			};

			var paymentTask = _firebaseClient.UpdateDataAsync<Payment>("Payments", info, payment);

			await Task.WhenAll(paymentTask, regTask);

			StartDeletionTimer(customerId, info, "Gym", regisId, "");

			return new RegisterResult
			{
				Membership = membership.Result,
				MoneyToPay = payment.Amount,
				TransactionContent = info
			};
			//return (membership, registration, payment, info);
		}

		public async Task<RegisterResult> RegisterTrainerRental(RegisterPackageRequest request,
	bool qrPayment, string customerId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			if (string.IsNullOrEmpty(request.SelectedTimeSlot))
			{
				throw new ArgumentException("The selected time slot is null");
			}

			if (!request.Duration.HasValue || request.Duration == 0)
			{
				throw new ArgumentException("Invalid months or sessions");
			}

			var plan = await _firebaseClient
							.Child("TrainerRentalPlans")
							.Child(request.TrainerRentalPlanId)
							.OnceSingleAsync<TrainerRentalPlan>();

			if (plan == null)
			{
				throw new InvalidOperationException("trainer plan is not valid");
			}

			var option = await _firebaseClient
							.Child("RentalOptions")
							.Child(plan.RentalOptionId)
							.OnceSingleAsync<RentalOption>();

			decimal price = 0;
			if (request.Emails.Count == option.MemberCount)
			{
				if (option.SessionCountMax == 0 && option.SessionCountMin == 0)
				{
					price = (decimal)(option.PricePerPersonPerMonth * request.Duration * request.Emails.Count);
				}
				else
				{
					if (request.Duration >= option.SessionCountMin && request.Duration <= option.SessionCountMax)
					{
						price = (decimal)(option.PricePerPersonPerSession * request.Duration * request.Emails.Count);
					}
					else
					{
						throw new ArgumentException("Number of sessions is not valid to register this membership");
					}
				}
			}
			else
			{
				throw new ArgumentException("Number of emails is not valid to register this membership");
			}

			// Check for duplicate emails
			var emailDuplicates = request.Emails.GroupBy(e => e)
												 .Where(g => g.Count() > 1)
												 .Select(g => g.Key)
												 .ToList();

			if (emailDuplicates.Any())
			{
				throw new InvalidOperationException("Do not enter duplicate emails");
			}

			// Fetch all userIds for emails
			var userTasks = request.Emails.Select(email => _firebaseAuth.GetUserByEmailAsync(email));
			var userResults = await Task.WhenAll(userTasks);
			var allVerified = userResults.All(a => a.EmailVerified);

			if (!allVerified)
			{
				throw new InvalidOperationException("Some emails are not verified.");
			}

			var userIds = userResults.Select(a => a.Uid).Where(id => !string.IsNullOrEmpty(id)).ToList();

			// Ensure all users exist
			if (userIds.Count != request.Emails.Count)
			{
				throw new InvalidOperationException("Some users are not registered.");
			}

			// Fetch roles for all userIds
			var roleTasks = userIds.Select(userId => _roleService.GetRoleOfUser(userId));
			var roleResults = await Task.WhenAll(roleTasks);

			// Validate roles
			if (roleResults.Any(role => !role.Equals("customer")))
			{
				throw new UnauthorizedAccessException("One or more users do not have the required 'customer' role.");
			}

			// Query for active registrations
			var activeRentalRegistrations = await _firebaseClient
				.Child("TrainerRentalRegistrations")
				.OrderBy("isActive")
				.EqualTo(true)
				.OnceAsync<TrainerRentalRegistration>();

			if (activeRentalRegistrations.Count != 0)
			{
				// Check if any user has active registrations
				var hasActiveRegistration = activeRentalRegistrations.Any(reg =>
					reg.Object.UserIds != null &&
					reg.Object.UserIds.Split(',').Any(userId => userIds.Contains(userId)) &&
					(reg.Object.EndDate >= DateTime.Now || reg.Object.SessionLeft > 0)
				);

				if (hasActiveRegistration)
				{
					throw new InvalidOperationException("One or more users already have active Trainer Rental Registrations.");
				}
			}

			// Build comma-separated userIds for storage
			var userIdsString = string.Join(",", userIds);

			RegisterScheduleRequest scheduleRequest = new RegisterScheduleRequest()
			{
				BoxingMembershipPlanId = request.BoxingMembershipPlanId,
				TrainerRentalPlanId = request.TrainerRentalPlanId,
				Duration = request.Duration,
				Emails = request.Emails,
				IsMonWedFri = request.IsMonWedFri,
				SelectedTimeSlotId = request.SelectedTimeSlot,
			};

			var schedule = await _scheduleService.CreateSchedule(scheduleRequest, userIdsString);

			var scheduleId = schedule.Item1;

			var checkMembershipTask = userIds.Select(userId => _gymMembershipCheckService.CheckGymMembershipEndDate(userId, schedule.Item2));
			var hasMembership = await Task.WhenAll(checkMembershipTask);

			if (hasMembership.Any(membership => !membership))
			{
				throw new InvalidOperationException("User doesn't have an active gym membership");
			}

			int monthToAdd = 0;
			int sessionToAdd = 0;

			if (option.SessionCountMax == 0 && option.SessionCountMin == 0)
			{
				monthToAdd = request.Duration.Value;
			}
			else
			{
				sessionToAdd = request.Duration.Value;
			}

			// Prepare the request data
			string info = "";

			if (qrPayment)
			{
				info = "SEVQR" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}
			else
			{
				info = "TM" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}

			var regisId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);

			TrainerRentalRegistration trainerRegistration = new TrainerRentalRegistration()
			{
				UserIds = userIdsString,
				PlanId = request.TrainerRentalPlanId,
				ScheduleId = scheduleId,
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddMonths(monthToAdd),
				SessionLeft = sessionToAdd,
				IsActive = false,
				PaymentId = info
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = JsonSerializer.Serialize(trainerRegistration, options);

			var regTask = _firebaseClient
							.Child("TrainerRentalRegistrations")
							.Child(regisId)
							.PutAsync(jsonString);

			string paymentMethod = qrPayment ? "QR" : "cash";

			Payment payment = new Payment()
			{
				TrainerRentalRegistrationId = regisId,
				Amount = price,
				GymRegistrationId = "",
				BoxingRegistrationId = "",
				PaymentDate = DateTime.MinValue,
				PaymentMethod = paymentMethod,
				PaymentStatus = "Pending",
				TransactionId = "Pending",
			};

			jsonString = JsonSerializer.Serialize(payment, options);

			var paymentTask = _firebaseClient
				.Child("Payments")
				.Child(info)
				.PutAsync(jsonString);

			await Task.WhenAll(paymentTask, regTask);

			StartDeletionTimer(customerId, info, "TrainerRental", regisId, scheduleId);

			return new RegisterResult
			{
				RentalPlan = plan,
				RentalOption = option,
				MoneyToPay = payment.Amount,
				TransactionContent = info
			};
		}

		public async Task<RegisterResult> RegisterBoxing(RegisterPackageRequest request,
			bool qrPayment, string customerId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
			if (string.IsNullOrEmpty(request.SelectedTimeSlot))
			{
				throw new ArgumentException("The selected time slot is null");
			}

			var plan = await _firebaseClient
							.Child("BoxingMembershipPlans")
							.Child(request.BoxingMembershipPlanId)
							.OnceSingleAsync<BoxingMembershipPlan>();

			if (plan == null)
			{
				throw new InvalidOperationException("boxing plan is not valid");
			}

			var option = await _firebaseClient
							.Child("BoxingOptions")
							.Child(plan.BoxingOptionId)
							.OnceSingleAsync<BoxingOption>();

			decimal price = 0;
			if (request.Emails.Count == option.MemberCount)
			{
				price = option.TotalPrice;
			}
			else
			{
				throw new ArgumentException("Number of emails does not match the package");
			}
			// Check for duplicate emails
			var emailDuplicates = request.Emails.GroupBy(e => e)
												 .Where(g => g.Count() > 1)
												 .Select(g => g.Key)
												 .ToList();

			if (emailDuplicates.Any())
			{
				throw new InvalidOperationException("Do not enter duplicate emails");
			}
			#region old code to check existing registrations
			//StringBuilder userIds = new StringBuilder();
			//// Get all TrainerRentalRegistrations for this user
			//var existingRegistrations = await _firebaseClient
			//	.Child("BoxingRegistrations")
			//	.OnceAsync<BoxingRegistration>();

			//// Check if there are any registrations that contain the target userId and are still active
			//bool hasActiveRegistration = false;

			//foreach (var email in request.Emails)
			//{
			//	var userId = await _emailService.GetUserIdByEmail(email);
			//	hasActiveRegistration = existingRegistrations.Any(reg =>
			//	reg.Object.UserIds != null &&
			//	reg.Object.UserIds.Split(',').Contains(userId) &&
			//	(reg.Object.SessionLeft > 0)
			//	);
			//	if (userId == null)
			//	{
			//		throw new InvalidOperationException($"No user found with the email {email}. The user must be registered.");
			//	}
			//	var roleName = await _roleService.GetRoleOfUser(userId);
			//	if (!roleName.Equals("customer"))
			//	{
			//		throw new UnauthorizedAccessException("User does not have the required 'customer' role.");
			//	}
			//	if (hasActiveRegistration)
			//	{
			//		throw new InvalidOperationException("The user already has an active Boxing Registration.");
			//	}
			//	userIds.Append(userId).Append(",");
			//}

			//// Continue with the registration process

			//if (userIds.Length > 0)
			//{
			//	userIds.Length--; // This removes the last comma
			//}

			//string userIdsString = userIds.ToString();
			#endregion

			// Fetch all userIds for emails
			var userTasks = request.Emails.Select(email => _firebaseAuth.GetUserByEmailAsync(email));
			var userResults = await Task.WhenAll(userTasks);
			var allVerified = userResults.All(a => a.EmailVerified);

			if (!allVerified)
			{
				throw new InvalidOperationException("Some emails are not verified.");
			}
			var userIds = userResults.Select(a => a.Uid).Where(id => !string.IsNullOrEmpty(id)).ToList();

			// Ensure all users exist
			if (userIds.Count != request.Emails.Count)
			{
				throw new InvalidOperationException("Some users are not registered.");
			}

			// Fetch roles for all userIds
			var roleTasks = userIds.Select(userId => _roleService.GetRoleOfUser(userId));
			var roleResults = await Task.WhenAll(roleTasks);

			// Validate roles
			if (roleResults.Any(role => !role.Equals("customer")))
			{
				throw new UnauthorizedAccessException("One or more users do not have the required 'customer' role.");
			}

			// Query for active registrations
			var activeRegistrations = await _firebaseClient
				.Child("BoxingRegistrations")
				.OrderBy("isActive")
				.EqualTo(true)
				.OnceAsync<BoxingRegistration>();

			if (activeRegistrations.Count != 0)
			{
				// Check if any user has active registrations
				var hasActiveRegistration = activeRegistrations.Any(reg =>
				reg.Object.UserIds != null &&
				reg.Object.UserIds.Split(',').Any(userId => userIds.Contains(userId)) &&
				(reg.Object.SessionLeft > 0 && reg.Object.EndDate >= DateTime.Now)
			);

				if (hasActiveRegistration)
				{
					throw new InvalidOperationException("One or more users already have active Boxing Registrations.");
				}
			}

			RegisterScheduleRequest scheduleRequest = new RegisterScheduleRequest()
			{
				BoxingMembershipPlanId = request.BoxingMembershipPlanId,
				TrainerRentalPlanId = request.TrainerRentalPlanId,
				Duration = request.Duration,
				Emails = request.Emails,
				IsMonWedFri = request.IsMonWedFri,
				SelectedTimeSlotId = request.SelectedTimeSlot,
			};
			var schedule = await _scheduleService.CreateSchedule(scheduleRequest, userIdsString);
			var scheduleId = schedule.Item1;

			// Build comma-separated userIds for storage
			var userIdsString = string.Join(",", userIds);

			var checkMembershipTask = userIds.Select(userId => _gymMembershipCheckService.CheckGymMembershipEndDate(userId, schedule.Item2));
			var hasMembership = await Task.WhenAll(checkMembershipTask);

			if (hasMembership.Any(membership => !membership))
			{
				throw new InvalidOperationException("User doesn't have an active gym membership");
			}

			// Prepare the request data
			string info = "";

			if (qrPayment)
			{
				info = "SEVQR" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}
			else
			{
				info = "TM" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
			}

			var regisId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);

			BoxingRegistration boxingRegistration = new BoxingRegistration()
			{
				UserIds = userIdsString,
				ScheduleId = scheduleId,
				BoxingMembershipPlanId = request.BoxingMembershipPlanId,
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddMonths(option.Months),
				SessionLeft = option.Sessions,
				IsActive = false,
				PaymentId = info
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = JsonSerializer.Serialize(boxingRegistration, options);

			var regTask = _firebaseClient
							.Child("BoxingRegistrations")
							.Child(regisId)
							.PutAsync(jsonString);

			//var methods = await _paymentMethodService.GetAllPaymentMethods();
			string paymentMethod = qrPayment ? "QR" : "cash";
			//string qrPaymentMethodId = methods.FirstOrDefault(method => method.MethodName == paymentMethod)?.PaymentMethodId;

			Payment payment = new Payment()
			{
				BoxingRegistrationId = regisId,
				Amount = price,
				TrainerRentalRegistrationId = "",
				GymRegistrationId = "",
				PaymentDate = DateTime.MinValue,
				PaymentMethod = paymentMethod,
				PaymentStatus = "Pending",
				TransactionId = "Pending",
			};

			jsonString = JsonSerializer.Serialize(payment, options);

			var paymentTask = _firebaseClient
				.Child("Payments")
				.Child(info)
				.PutAsync(jsonString);

			await Task.WhenAll(paymentTask, regTask);

			StartDeletionTimer(customerId, info, "Boxing", regisId, scheduleId);

			return new RegisterResult
			{
				BoxingPlan = plan,
				BoxingOption = option,
				MoneyToPay = payment.Amount,
				TransactionContent = info
			};

			//return (plan, option, registration, payment, info);
		}

		private void StartDeletionTimer(string customerId, string paymentId, string registrationType, string registrationId, string scheduleId)
		{
			// If there's already a timer for this customer, stop and dispose it
			if (_customerTimers.TryGetValue(customerId, out var existingTimer))
			{
				existingTimer.Stop();
				existingTimer.Dispose();
			}

			// Track the registration details
			_pendingRegistrations[customerId] = (paymentId, registrationType, registrationId, scheduleId);

			// Create and start a new timer
			var timer = new System.Timers.Timer(paymentWaitingTime);
			timer.Elapsed += async (sender, e) => await DeleteMembershipsAsync(customerId);
			timer.AutoReset = false; // Run only once
			timer.Start();

			_customerTimers[customerId] = timer; // Track the timer
		}


		private async Task DeleteMembershipsAsync(string customerId)
		{
			if (!_pendingRegistrations.TryGetValue(customerId, out var registrationDetails))
				return;

			var (paymentId, registrationType, registrationId, scheduleId) = registrationDetails;

			try
			{
				// Check payment status
				var createdPayment = await _firebaseClient.Child("Payments").Child(paymentId).OnceSingleAsync<Payment>();
				if (createdPayment != null && !createdPayment.PaymentStatus.Equals("Completed"))
				{
					// Prepare deletion tasks
					var tasks = new List<Task>
					{
						_firebaseClient.Child("Payments").Child(paymentId).DeleteAsync()
					};

					tasks.Add(_firebaseClient.Child($"{registrationType}Registrations").Child(registrationId).DeleteAsync());
					if (!string.IsNullOrEmpty(scheduleId))
					{
						tasks.Add(_firebaseClient.Child("Schedules").Child(scheduleId).DeleteAsync());

						// Handle slot deletions
						var slots = await _firebaseClient.Child("Slots")
							.OrderBy("scheduleId").EqualTo(scheduleId).OnceAsync<Slot>();
						tasks.AddRange(slots.Select(slot => _firebaseClient.Child("Slots").Child(slot.Key).DeleteAsync()));
					}

					// Execute all deletions
					await Task.WhenAll(tasks);
				}
			}
			catch (Exception ex)
			{
				// Log or handle exception as needed
				Console.WriteLine($"Error during deletion for customer {customerId}: {ex.Message}");
			}

			// Clean up state
			_pendingRegistrations.Remove(customerId);
			_customerTimers.Remove(customerId);
		}

	}
}
