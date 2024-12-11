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
			_paymentMethodService = paymentMethodService;
			_emailService = emailService;
			_roleService = roleService;
			_scheduleService = scheduleService;
			_gymMembershipCheckService = gymMembershipCheckService;
			_firebaseAuth = firebaseAuth;
		}
		public async Task<RegisterResult> RegisterGym(RegisterPackageRequest request, bool qrPayment, string customerId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

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
				exReg.Object.IsActive &&
				(exReg.Object.EndDate >= DateTime.Now || exReg.Object.SessionLeft > 0));

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


			GymRegistration gymRegistration = new GymRegistration()
			{
				UserId = userId,
				GymMembershipId = request.GymMembershipId,
				StartDate = DateTime.Now,
				EndDate = DateTime.Now.AddMonths(membership.Result.DurationMonths ?? 0),
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

			jsonString = JsonSerializer.Serialize(payment, options);

			var paymentTask = _firebaseClient
				.Child("Payments")
				.Child(info)
				.PutAsync(jsonString);

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

            try
            {
                // Bước 1: Kiểm tra thời gian đã chọn
                if (string.IsNullOrEmpty(request.SelectedTimeSlot))
                {
                    throw new ArgumentException("The selected time slot is null");
                }

                // Bước 2: Kiểm tra Duration hợp lệ
                if (!request.Duration.HasValue || request.Duration == 0)
                {
                    throw new ArgumentException("Invalid months or sessions");
                }

                // Bước 3: Lấy thông tin TrainerRentalPlan
                var plan = await _firebaseClient
                                .Child("TrainerRentalPlans")
                                .Child(request.TrainerRentalPlanId)
                                .OnceSingleAsync<TrainerRentalPlan>();

                if (plan == null)
                {
                    throw new InvalidOperationException("Trainer plan is not valid");
                }

                // Bước 4: Lấy thông tin RentalOption
                var option = await _firebaseClient
                                .Child("RentalOptions")
                                .Child(plan.RentalOptionId)
                                .OnceSingleAsync<RentalOption>();

                if (option == null)
                {
                    throw new InvalidOperationException("Rental option is not valid");
                }

                // Bước 5: Tính toán giá
                decimal price = 0;
                if (request.Emails.Count == option.MemberCount)
                {
                    if (option.SessionCountMax == 0 && option.SessionCountMin == 0)
                    {
                        price = (decimal)(option.PricePerPersonPerMonth * request.Duration.Value * request.Emails.Count);
                    }
                    else
                    {
                        if (request.Duration.Value >= option.SessionCountMin && request.Duration.Value <= option.SessionCountMax)
                        {
                            price = (decimal)(option.PricePerPersonPerSession * request.Duration.Value * request.Emails.Count);
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

                // Bước 6: Kiểm tra trùng lặp email
                var emailDuplicates = request.Emails.GroupBy(e => e)
                                                     .Where(g => g.Count() > 1)
                                                     .Select(g => g.Key)
                                                     .ToList();

                if (emailDuplicates.Any())
                {
                    throw new InvalidOperationException("Do not enter duplicate emails");
                }

                // Bước 7: Lấy userIds từ email và kiểm tra
                var userTasks = request.Emails.Select(email => _firebaseAuth.GetUserByEmailAsync(email));
                var userResults = await Task.WhenAll(userTasks);
                var allVerified = userResults.All(a => a.EmailVerified);

                if (!allVerified)
                {
                    throw new InvalidOperationException("Some emails are not verified.");
                }

                var userIds = userResults.Select(a => a.Uid).Where(id => !string.IsNullOrEmpty(id)).ToList();

                // Bước 8: Đảm bảo tất cả người dùng tồn tại
                if (userIds.Count != request.Emails.Count)
                {
                    throw new InvalidOperationException("Some users are not registered.");
                }

                // Bước 9: Lấy và kiểm tra vai trò của người dùng
                var roleTasks = userIds.Select(userId => _roleService.GetRoleOfUser(userId));
                var roleResults = await Task.WhenAll(roleTasks);

                if (roleResults.Any(role => !role.Equals("customer", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new UnauthorizedAccessException("One or more users do not have the required 'customer' role.");
                }

                // Bước 10: Kiểm tra đăng ký đang hoạt động trước khi tạo lịch trình
                var activeRentalRegistrations = await _firebaseClient
                    .Child("TrainerRentalRegistrations")
                    .OrderBy("isActive")
                    .EqualTo(true)
                    .OnceAsync<TrainerRentalRegistration>();

                if (activeRentalRegistrations.Count != 0)
                {
                    // Kiểm tra xem có người dùng nào có đăng ký hoạt động không
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

                // Bước 11: Kiểm tra thành viên gym
                var checkMembershipTask = userIds.Select(userId => _gymMembershipCheckService.CheckGymMembership(userId));
                var hasMembership = await Task.WhenAll(checkMembershipTask);

                if (hasMembership.Any(membership => !membership))
                {
                    throw new InvalidOperationException("User doesn't have an active gym membership");
                }

                // Bước 12: Xây dựng chuỗi userIds
                var userIdsString = string.Join(",", userIds);

                // Tất cả các kiểm tra đã thành công, bây giờ gọi CreateSchedule
                RegisterScheduleRequest scheduleRequest = new RegisterScheduleRequest()
                {
                    BoxingMembershipPlanId = request.BoxingMembershipPlanId,
                    TrainerRentalPlanId = request.TrainerRentalPlanId,
                    Duration = request.Duration,
                    Emails = request.Emails,
                    IsMonWedFri = request.IsMonWedFri,
                    SelectedTimeSlotId = request.SelectedTimeSlot,
                };

                var scheduleId = await _scheduleService.CreateSchedule(scheduleRequest, userIdsString);

                // Bước 13: Chuẩn bị dữ liệu yêu cầu
                string info = qrPayment ? "SEVQR" + Guid.NewGuid().ToString("N").Substring(0, 15)
                                         : "TM" + Guid.NewGuid().ToString("N").Substring(0, 15);

                string regisId = Guid.NewGuid().ToString("N").Substring(0, 15);

                TrainerRentalRegistration trainerRegistration = new TrainerRentalRegistration()
                {
                    UserIds = userIdsString,
                    PlanId = request.TrainerRentalPlanId,
                    ScheduleId = scheduleId,
                    StartDate = DateTime.Now,
                    EndDate = option.SessionCountMax == 0 && option.SessionCountMin == 0
                             ? DateTime.Now.AddMonths(request.Duration.Value)
                             : DateTime.Now.AddMonths(0), // Nếu không phải theo tháng, giữ EndDate không thay đổi
                    SessionLeft = option.SessionCountMax == 0 && option.SessionCountMin == 0
                                  ? 0
                                  : request.Duration.Value,
                    IsActive = false,
                    PaymentId = info
                };

                var optionsSerializer = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var trainerRegistrationJson = JsonSerializer.Serialize(trainerRegistration, optionsSerializer);

                var regTask = _firebaseClient
                                .Child("TrainerRentalRegistrations")
                                .Child(regisId)
                                .PutAsync(trainerRegistrationJson);

                // Bước 14: Tạo thanh toán
                string paymentMethod = qrPayment ? "QR" : "cash";

                Payment payment = new Payment()
                {
                    TrainerRentalRegistrationId = regisId,
                    Amount = price,
                    GymRegistrationId = "",
                    BoxingRegistrationId = "",
                    PaymentDate = DateTime.MinValue, // Cập nhật ngày thực tế nếu cần
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "Pending",
                    TransactionId = "Pending",
                };

                var paymentJson = JsonSerializer.Serialize(payment, optionsSerializer);

                var paymentTask = _firebaseClient
                    .Child("Payments")
                    .Child(info)
                    .PutAsync(paymentJson);

                await Task.WhenAll(paymentTask, regTask);

                // Bước 15: Khởi động hẹn giờ xóa (nếu cần)
                StartDeletionTimer(customerId, info, "TrainerRental", regisId, scheduleId);

                return new RegisterResult
                {
                    RentalPlan = plan,
                    RentalOption = option,
                    MoneyToPay = payment.Amount,
                    TransactionContent = info
                };
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết (ví dụ: sử dụng ILogger)
                // _logger.LogError(ex, "Error during trainer rental registration");

                // Trả về thông báo lỗi phù hợp hoặc tái ném ngoại lệ
                throw; // Hoặc trả về một kết quả lỗi tùy theo thiết kế API của bạn
            }
        }


        public async Task<RegisterResult> RegisterBoxing(RegisterPackageRequest request,
            bool qrPayment, string customerId)
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

            try
            {
                // Bước 1: Kiểm tra thời gian đã chọn
                if (string.IsNullOrEmpty(request.SelectedTimeSlot))
                {
                    throw new ArgumentException("The selected time slot is null");
                }

                // Bước 2: Lấy thông tin BoxingMembershipPlan
                var plan = await _firebaseClient
                                .Child("BoxingMembershipPlans")
                                .Child(request.BoxingMembershipPlanId)
                                .OnceSingleAsync<BoxingMembershipPlan>();

                if (plan == null)
                {
                    throw new InvalidOperationException("Boxing plan is not valid");
                }

                // Bước 3: Lấy thông tin BoxingOption
                var option = await _firebaseClient
                                .Child("BoxingOptions")
                                .Child(plan.BoxingOptionId)
                                .OnceSingleAsync<BoxingOption>();

                if (option == null)
                {
                    throw new InvalidOperationException("Boxing option is not valid");
                }

                // Bước 4: Tính toán giá
                decimal price = 0;
                if (request.Emails.Count == option.MemberCount)
                {
                    price = option.TotalPrice;
                }
                else
                {
                    throw new ArgumentException("Number of emails does not match the package");
                }

                // Bước 5: Kiểm tra trùng lặp email
                var emailDuplicates = request.Emails.GroupBy(e => e)
                                                     .Where(g => g.Count() > 1)
                                                     .Select(g => g.Key)
                                                     .ToList();

                if (emailDuplicates.Any())
                {
                    throw new InvalidOperationException("Do not enter duplicate emails");
                }

                // Bước 6: Lấy userIds từ email và kiểm tra
                var userTasks = request.Emails.Select(email => _firebaseAuth.GetUserByEmailAsync(email));
                var userResults = await Task.WhenAll(userTasks);
                var allVerified = userResults.All(a => a.EmailVerified);

                if (!allVerified)
                {
                    throw new InvalidOperationException("Some emails are not verified.");
                }

                var userIds = userResults.Select(a => a.Uid).Where(id => !string.IsNullOrEmpty(id)).ToList();

                // Bước 7: Đảm bảo tất cả người dùng tồn tại
                if (userIds.Count != request.Emails.Count)
                {
                    throw new InvalidOperationException("Some users are not registered.");
                }

                // Bước 8: Lấy và kiểm tra vai trò của người dùng
                var roleTasks = userIds.Select(userId => _roleService.GetRoleOfUser(userId));
                var roleResults = await Task.WhenAll(roleTasks);

                if (roleResults.Any(role => !role.Equals("customer", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new UnauthorizedAccessException("One or more users do not have the required 'customer' role.");
                }

                // Bước 9: Xây dựng chuỗi userIds
                var userIdsString = string.Join(",", userIds);

                // Bước 10: Kiểm tra đăng ký đang hoạt động trước khi tạo lịch trình
                var activeRegistrations = await _firebaseClient
                    .Child("BoxingRegistrations")
                    .OrderBy("isActive")
                    .EqualTo(true)
                    .OnceAsync<BoxingRegistration>();

                if (activeRegistrations.Count != 0)
                {
                    // Kiểm tra xem có người dùng nào có đăng ký hoạt động không
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

                // Bước 11: Tạo lịch trình
                RegisterScheduleRequest scheduleRequest = new RegisterScheduleRequest()
                {
                    BoxingMembershipPlanId = request.BoxingMembershipPlanId,
                    TrainerRentalPlanId = request.TrainerRentalPlanId,
                    Duration = request.Duration,
                    Emails = request.Emails,
                    IsMonWedFri = request.IsMonWedFri,
                    SelectedTimeSlotId = request.SelectedTimeSlot,
                };

                var scheduleId = await _scheduleService.CreateSchedule(scheduleRequest, userIdsString);

                // Bước 12: Chuẩn bị dữ liệu yêu cầu
                string info = qrPayment ? "SEVQR" + Guid.NewGuid().ToString("N").Substring(0, 15)
                                         : "TM" + Guid.NewGuid().ToString("N").Substring(0, 15);

                string regisId = Guid.NewGuid().ToString("N").Substring(0, 15);

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

                var optionsSerializer = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var boxingRegistrationJson = JsonSerializer.Serialize(boxingRegistration, optionsSerializer);

                var regTask = _firebaseClient
                                .Child("BoxingRegistrations")
                                .Child(regisId)
                                .PutAsync(boxingRegistrationJson);

                // Bước 13: Tạo thanh toán
                string paymentMethod = qrPayment ? "QR" : "cash";

                Payment payment = new Payment()
                {
                    BoxingRegistrationId = regisId,
                    Amount = price,
                    TrainerRentalRegistrationId = "",
                    GymRegistrationId = "",
                    PaymentDate = DateTime.MinValue, // Cập nhật ngày thực tế nếu cần
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "Pending",
                    TransactionId = "Pending",
                };

                var paymentJson = JsonSerializer.Serialize(payment, optionsSerializer);

                var paymentTask = _firebaseClient
                    .Child("Payments")
                    .Child(info)
                    .PutAsync(paymentJson);

                await Task.WhenAll(paymentTask, regTask);

                // Bước 14: Khởi động hẹn giờ xóa (nếu cần)
                StartDeletionTimer(customerId, info, "Boxing", regisId, scheduleId);

                return new RegisterResult
                {
                    BoxingPlan = plan,
                    BoxingOption = option,
                    MoneyToPay = payment.Amount,
                    TransactionContent = info
                };
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết (ví dụ: sử dụng ILogger)
                // _logger.LogError(ex, "Error during boxing registration");

                // Trả về thông báo lỗi phù hợp hoặc tái ném ngoại lệ
                throw; // Hoặc trả về một kết quả lỗi tùy theo thiết kế API của bạn
            }
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
