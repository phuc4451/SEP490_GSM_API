using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Alpha_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentHistoryController : ControllerBase
    {
        private readonly FirebaseClient _firebaseClient;

        public PaymentHistoryController(FirebaseClientProvider firebaseClientProvider)
        {
            _firebaseClient = firebaseClientProvider.GetFirebaseClient();
        }

        [HttpGet("{userId}")]
        //[Authorize(Roles = "admin,staff,customer")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaymentHistory(string userId)
        {
            var paymentHistory = new List<object>();

            // Fetch Gym Registrations
            var gymRegistrations = await _firebaseClient
                .Child("GymRegistrations")
                .OrderBy("userId")
                .EqualTo(userId)
                .OnceAsync<GymRegistration>();

            // Fetch Payments and Gym Memberships in parallel
            var paymentIds = gymRegistrations.Select(reg => reg.Object.PaymentId).ToList();
            var gymMembershipIds = gymRegistrations.Select(reg => reg.Object.GymMembershipId).ToList();

            var paymentsTask = _firebaseClient
                .Child("Payments")
                .OrderByKey()
                .OnceAsync<Payment>()
                .ContinueWith(t => t.Result.ToDictionary(p => p.Key, p => p.Object));

            var gymMembershipsTask = _firebaseClient
                .Child("GymMemberships")
                .OrderByKey()
                .OnceAsync<GymMembership>()
                .ContinueWith(t => t.Result.ToDictionary(p => p.Key, p => p.Object));

            await Task.WhenAll(paymentsTask, gymMembershipsTask);

            var payments = paymentsTask.Result;
            var gymMemberships = gymMembershipsTask.Result;

            foreach (var reg in gymRegistrations)
            {
                var payment = payments.ContainsKey(reg.Object.PaymentId) ? payments[reg.Object.PaymentId] : null;
                var gymMembership = gymMemberships.ContainsKey(reg.Object.GymMembershipId) ? gymMemberships[reg.Object.GymMembershipId] : null;

                paymentHistory.Add(new
                {
                    PackageName = gymMembership?.Name ?? "Unknown Package",
                    Amount = payment?.Amount ?? 0,
                    PaymentStatus = payment?.PaymentStatus ?? "Unknown",
                    StartDate = reg.Object.StartDate,
                    EndDate = reg.Object.EndDate
                });
            }

            // Fetch Boxing Registrations
            var boxingRegistrations = await _firebaseClient
                .Child("BoxingRegistrations")
                .OrderBy("userIds")
                .EqualTo(userId)
                .OnceAsync<BoxingRegistration>();

            // Fetch Payments and Boxing Membership Plans in parallel
            var boxingPaymentIds = boxingRegistrations.Select(reg => reg.Object.PaymentId).ToList();
            var boxingMembershipPlanIds = boxingRegistrations.Select(reg => reg.Object.BoxingMembershipPlanId).ToList();

            var boxingPaymentsTask = _firebaseClient
                .Child("Payments")
                .OrderByKey()
                .OnceAsync<Payment>()
                .ContinueWith(t => t.Result.ToDictionary(p => p.Key, p => p.Object));

            var boxingMembershipPlansTask = _firebaseClient
                .Child("BoxingMembershipPlans")
                .OrderByKey()
                .OnceAsync<BoxingMembershipPlan>()
                .ContinueWith(t => t.Result.ToDictionary(p => p.Key, p => p.Object));

            var boxingOptionsTask = _firebaseClient
                .Child("BoxingOptions")
                .OrderByKey()
                .OnceAsync<BoxingOption>()
                .ContinueWith(t => t.Result.ToDictionary(p => p.Key, p => p.Object));

            await Task.WhenAll(boxingPaymentsTask, boxingMembershipPlansTask, boxingOptionsTask);

            var boxingPayments = boxingPaymentsTask.Result;
            var boxingMembershipPlans = boxingMembershipPlansTask.Result;
            var boxingOptions = boxingOptionsTask.Result;

            foreach (var reg in boxingRegistrations)
            {
                var payment = boxingPayments.ContainsKey(reg.Object.PaymentId) ? boxingPayments[reg.Object.PaymentId] : null;
                var boxingMembershipPlan = boxingMembershipPlans.ContainsKey(reg.Object.BoxingMembershipPlanId) ? boxingMembershipPlans[reg.Object.BoxingMembershipPlanId] : null;
                var boxingOption = boxingOptions.ContainsKey(boxingMembershipPlan?.BoxingOptionId) ? boxingOptions[boxingMembershipPlan.BoxingOptionId] : null;

                paymentHistory.Add(new
                {
                    PackageName = boxingOption?.Description ?? "Unknown Package",
                    Amount = payment?.Amount ?? 0,
                    PaymentStatus = payment?.PaymentStatus ?? "Unknown",
                    StartDate = reg.Object.StartDate,
                    EndDate = reg.Object.EndDate
                });
            }

            // Fetch Trainer Rental Registrations
            var trainerRentalRegistrations = await _firebaseClient
                .Child("TrainerRentalRegistrations")
                .OrderBy("userIds")
                .EqualTo(userId)
                .OnceAsync<TrainerRentalRegistration>();

            // Fetch Payments and Trainer Rental Plans in parallel
            var trainerRentalPaymentIds = trainerRentalRegistrations.Select(reg => reg.Object.PaymentId).ToList();
            var trainerRentalPlanIds = trainerRentalRegistrations.Select(reg => reg.Object.PlanId).ToList();

            var trainerRentalPaymentsTask = _firebaseClient
                .Child("Payments")
                .OrderByKey()
                .OnceAsync<Payment>()
                .ContinueWith(t => t.Result.ToDictionary(p => p.Key, p => p.Object));

            var trainerRentalPlansTask = _firebaseClient
                .Child("TrainerRentalPlans")
                .OrderByKey()
                .OnceAsync<TrainerRentalPlan>()
                .ContinueWith(t => t.Result.ToDictionary(p => p.Key, p => p.Object));

            var rentalOptionsTask = _firebaseClient
                .Child("RentalOptions")
                .OrderByKey()
                .OnceAsync<RentalOption>()
                .ContinueWith(t => t.Result.ToDictionary(p => p.Key, p => p.Object));

            await Task.WhenAll(trainerRentalPaymentsTask, trainerRentalPlansTask, rentalOptionsTask);

            var trainerRentalPayments = trainerRentalPaymentsTask.Result;
            var trainerRentalPlans = trainerRentalPlansTask.Result;
            var rentalOptions = rentalOptionsTask.Result;

            foreach (var reg in trainerRentalRegistrations)
            {
                var payment = trainerRentalPayments.ContainsKey(reg.Object.PaymentId) ? trainerRentalPayments[reg.Object.PaymentId] : null;
                var trainerRentalPlan = trainerRentalPlans.ContainsKey(reg.Object.PlanId) ? trainerRentalPlans[reg.Object.PlanId] : null;
                var rentalOption = rentalOptions.ContainsKey(trainerRentalPlan?.RentalOptionId) ? rentalOptions[trainerRentalPlan.RentalOptionId] : null;

                paymentHistory.Add(new
                {
                    PackageName = rentalOption?.Description ?? "Unknown Package",
                    Amount = payment?.Amount ?? 0,
                    PaymentStatus = payment?.PaymentStatus ?? "Unknown",
                    StartDate = reg.Object.StartDate,
                    EndDate = reg.Object.EndDate
                });
            }

            return Ok(paymentHistory);
        }



    }

}
