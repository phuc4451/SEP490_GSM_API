using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentHistoryController : ControllerBase
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly ILogger<PaymentHistoryController> _logger;

        public PaymentHistoryController(FirebaseClientProvider firebaseClientProvider, ILogger<PaymentHistoryController> logger)
        {
            _firebaseClient = firebaseClientProvider.GetFirebaseClient();
            _logger = logger;
        }

        [HttpGet("{userId}")]
        //[Authorize(Roles = "admin,staff,customer")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaymentHistory(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetPaymentHistory called with null or empty userId.");
                return BadRequest("UserId cannot be null or empty.");
            }

            var paymentHistory = new List<object>();

            try
            {
                // Fetch all registration types in parallel
                var gymRegistrationsTask = _firebaseClient
                    .Child("GymRegistrations")
                    .OrderBy("userId")
                    .EqualTo(userId)
                    .OnceAsync<GymRegistration>();

                var boxingRegistrationsTask = _firebaseClient
                    .Child("BoxingRegistrations")
                    .OrderBy("userIds")
                    .EqualTo(userId)
                    .OnceAsync<BoxingRegistration>();

                var trainerRentalRegistrationsTask = _firebaseClient
                    .Child("TrainerRentalRegistrations")
                    .OrderBy("userIds")
                    .EqualTo(userId)
                    .OnceAsync<TrainerRentalRegistration>();

                // Await all registration fetch tasks
                await Task.WhenAll(gymRegistrationsTask, boxingRegistrationsTask, trainerRentalRegistrationsTask);

                var gymRegistrations = gymRegistrationsTask.Result.Select(r => r.Object).ToList();
                var boxingRegistrations = boxingRegistrationsTask.Result.Select(r => r.Object).ToList();
                var trainerRentalRegistrations = trainerRentalRegistrationsTask.Result.Select(r => r.Object).ToList();

                // Collect all Payment IDs to fetch Payments once
                var allPaymentIds = gymRegistrations.Select(reg => reg.PaymentId)
                                                  .Concat(boxingRegistrations.Select(reg => reg.PaymentId))
                                                  .Concat(trainerRentalRegistrations.Select(reg => reg.PaymentId))
                                                  .Where(id => !string.IsNullOrEmpty(id))
                                                  .Distinct()
                                                  .ToList();

                // Collect all GymMembershipIds
                var allGymMembershipIds = gymRegistrations.Select(reg => reg.GymMembershipId)
                                                         .Where(id => !string.IsNullOrEmpty(id))
                                                         .Distinct()
                                                         .ToList();

                // Collect all BoxingMembershipPlanIds
                var allBoxingMembershipPlanIds = boxingRegistrations.Select(reg => reg.BoxingMembershipPlanId)
                                                                     .Where(id => !string.IsNullOrEmpty(id))
                                                                     .Distinct()
                                                                     .ToList();

                // Collect all TrainerRentalPlanIds
                var allTrainerRentalPlanIds = trainerRentalRegistrations.Select(reg => reg.PlanId)
                                                                         .Where(id => !string.IsNullOrEmpty(id))
                                                                         .Distinct()
                                                                         .ToList();

                // Fetch Payments, GymMemberships, BoxingMembershipPlans, BoxingOptions, TrainerRentalPlans, RentalOptions in parallel
                var paymentsTask = _firebaseClient
                    .Child("Payments")
                    .OnceAsync<Payment>();

                var gymMembershipsTask = _firebaseClient
                    .Child("GymMemberships")
                    .OnceAsync<GymMembership>();

                var boxingMembershipPlansTask = _firebaseClient
                    .Child("BoxingMembershipPlans")
                    .OnceAsync<BoxingMembershipPlan>();

                var boxingOptionsTask = _firebaseClient
                    .Child("BoxingOptions")
                    .OnceAsync<BoxingOption>();

                var trainerRentalPlansTask = _firebaseClient
                    .Child("TrainerRentalPlans")
                    .OnceAsync<TrainerRentalPlan>();

                var rentalOptionsTask = _firebaseClient
                    .Child("RentalOptions")
                    .OnceAsync<RentalOption>();

                // Await all related data fetch tasks
                await Task.WhenAll(paymentsTask, gymMembershipsTask, boxingMembershipPlansTask, boxingOptionsTask, trainerRentalPlansTask, rentalOptionsTask);

                // Convert fetched data to dictionaries for quick lookup
                var paymentsDict = paymentsTask.Result
                                              .Where(p => allPaymentIds.Contains(p.Key))
                                              .ToDictionary(p => p.Key, p => p.Object);

                var gymMembershipsDict = gymMembershipsTask.Result
                                                        .Where(m => allGymMembershipIds.Contains(m.Key))
                                                        .ToDictionary(m => m.Key, m => m.Object);

                var boxingMembershipPlansDict = boxingMembershipPlansTask.Result
                                                                        .Where(b => allBoxingMembershipPlanIds.Contains(b.Key))
                                                                        .ToDictionary(b => b.Key, b => b.Object);

                var boxingOptionsDict = boxingOptionsTask.Result
                                                      .ToDictionary(b => b.Key, b => b.Object);

                var trainerRentalPlansDict = trainerRentalPlansTask.Result
                                                              .Where(t => allTrainerRentalPlanIds.Contains(t.Key))
                                                              .ToDictionary(t => t.Key, t => t.Object);

                var rentalOptionsDict = rentalOptionsTask.Result
                                                      .ToDictionary(r => r.Key, r => r.Object);

                // Process Gym Registrations
                foreach (var reg in gymRegistrations)
                {
                    if (reg == null) continue;

                    paymentsDict.TryGetValue(reg.PaymentId, out var payment);
                    gymMembershipsDict.TryGetValue(reg.GymMembershipId, out var gymMembership);

                    paymentHistory.Add(new
                    {
                        PackageName = gymMembership?.Name ?? "Unknown Package",
                        Amount = payment?.Amount ?? 0,
                        PaymentStatus = payment?.PaymentStatus ?? "Unknown",
                        StartDate = reg.StartDate,
                        EndDate = reg.EndDate
                    });
                }

                // Process Boxing Registrations
                foreach (var reg in boxingRegistrations)
                {
                    if (reg == null) continue;

                    paymentsDict.TryGetValue(reg.PaymentId, out var payment);
                    boxingMembershipPlansDict.TryGetValue(reg.BoxingMembershipPlanId, out var boxingPlan);
                    boxingOptionsDict.TryGetValue(boxingPlan?.BoxingOptionId, out var boxingOption);

                    paymentHistory.Add(new
                    {
                        PackageName = boxingOption?.Description ?? "Unknown Package",
                        Amount = payment?.Amount ?? 0,
                        PaymentStatus = payment?.PaymentStatus ?? "Unknown",
                        StartDate = reg.StartDate,
                        EndDate = reg.EndDate
                    });
                }

                // Process Trainer Rental Registrations
                foreach (var reg in trainerRentalRegistrations)
                {
                    if (reg == null) continue;

                    paymentsDict.TryGetValue(reg.PaymentId, out var payment);
                    trainerRentalPlansDict.TryGetValue(reg.PlanId, out var rentalPlan);
                    rentalOptionsDict.TryGetValue(rentalPlan?.RentalOptionId, out var rentalOption);

                    paymentHistory.Add(new
                    {
                        PackageName = rentalOption?.Description ?? "Unknown Package",
                        Amount = payment?.Amount ?? 0,
                        PaymentStatus = payment?.PaymentStatus ?? "Unknown",
                        StartDate = reg.StartDate,
                        EndDate = reg.EndDate
                    });
                }

                return Ok(paymentHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPaymentHistory for userId {userId}: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching payment history.");
            }
        }
    }
}
