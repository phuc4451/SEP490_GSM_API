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

            foreach (var reg in gymRegistrations)
            {
                var payment = await _firebaseClient
                    .Child("Payments")
                    .Child(reg.Object.PaymentId)
                    .OnceSingleAsync<Payment>();

                var gymMembership = await _firebaseClient
                    .Child("GymMemberships")
                    .Child(reg.Object.GymMembershipId)
                    .OnceSingleAsync<GymMembership>();

                paymentHistory.Add(new
                {
                    PackageName = gymMembership?.Name ?? "Unknown Package",
                    Amount = payment?.Amount ?? 0,
                    PaymentStatus = payment?.PaymentStatus ?? "Unknown"
                });
            }

            // Fetch Boxing Registrations
            var boxingRegistrations = await _firebaseClient
                .Child("BoxingRegistrations")
                .OrderBy("userIds")
                .EqualTo(userId)
                .OnceAsync<BoxingRegistration>();

            foreach (var reg in boxingRegistrations)
            {
                var payment = await _firebaseClient
                    .Child("Payments")
                    .Child(reg.Object.PaymentId)
                    .OnceSingleAsync<Payment>();

                // Lấy boxingMembershipPlanId từ BoxingRegistrations
                var boxingMembershipPlan = await _firebaseClient
                    .Child("BoxingMembershipPlans")
                    .Child(reg.Object.BoxingMembershipPlanId)
                    .OnceSingleAsync<BoxingMembershipPlan>();

                // Lấy boxingOptionId từ BoxingMembershipPlans
                var boxingOption = await _firebaseClient
                    .Child("BoxingOptions")
                    .Child(boxingMembershipPlan?.BoxingOptionId)
                    .OnceSingleAsync<BoxingOption>();

                paymentHistory.Add(new
                {
                    PackageName = boxingOption?.Description ?? "Unknown Package",
                    Amount = payment?.Amount ?? 0,
                    PaymentStatus = payment?.PaymentStatus ?? "Unknown"
                });
            }

            // Fetch Trainer Rental Registrations
            var trainerRentalRegistrations = await _firebaseClient
                .Child("TrainerRentalRegistrations")
                .OrderBy("userIds")
                .EqualTo(userId)
                .OnceAsync<TrainerRentalRegistration>();

            foreach (var reg in trainerRentalRegistrations)
            {
                var payment = await _firebaseClient
                    .Child("Payments")
                    .Child(reg.Object.PaymentId)
                    .OnceSingleAsync<Payment>();

                // Lấy rentalOptionId từ TrainerRentalPlans
                var trainerRentalPlan = await _firebaseClient
                    .Child("TrainerRentalPlans")
                    .Child(reg.Object.PlanId)
                    .OnceSingleAsync<TrainerRentalPlan>();

                var rentalOption = await _firebaseClient
                    .Child("RentalOptions")
                    .Child(trainerRentalPlan?.RentalOptionId)
                    .OnceSingleAsync<RentalOption>();

                paymentHistory.Add(new
                {
                    PackageName = rentalOption?.Description ?? "Unknown Package",
                    Amount = payment?.Amount ?? 0,
                    PaymentStatus = payment?.PaymentStatus ?? "Unknown"
                });
            }

            return Ok(paymentHistory);
        }






    }

}
