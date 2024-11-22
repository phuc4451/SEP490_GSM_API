using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Alpha_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackagesAndTrainersController : ControllerBase
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly FirebaseClientProvider _firebaseClientProvider;

        public PackagesAndTrainersController(FirebaseClientProvider firebaseClientProvider)
        {
            _firebaseClientProvider = firebaseClientProvider;
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();
        }

        // GET: api/packages-and-trainers?type=Boxing
        [HttpGet]
        public async Task<IActionResult> GetPackagesAndTrainers([FromQuery] string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return BadRequest("Type is required.");
            }

            List<object> packages = new List<object>();
            List<object> trainers = new List<object>();

            // Lấy danh sách gói tập
            if (type.Equals("Boxing", StringComparison.OrdinalIgnoreCase))
            {
                var boxingPackages = await _firebaseClient
                    .Child("BoxingOptions")
                    .OnceAsync<BoxingOption>();

                packages = boxingPackages.Select(pkg => new
                {
                    PackageId = pkg.Key,
                    pkg.Object.Description,
                    pkg.Object.Sessions,
                    pkg.Object.Months,
                    pkg.Object.MemberCount,
                    pkg.Object.TotalPrice
                }).Cast<object>().ToList(); // Chuyển kiểu vô danh thành object

            }
            else if (type.Equals("TrainerRental", StringComparison.OrdinalIgnoreCase))
            {
                var rentalPackages = await _firebaseClient
                    .Child("RentalOptions")
                    .OnceAsync<RentalOption>();

                packages = rentalPackages.Select(pkg => new
                {
                    PackageId = pkg.Key,
                    pkg.Object.Description,
                    pkg.Object.SessionCountMin,
                    pkg.Object.SessionCountMax,
                    pkg.Object.MemberCount,
                    PricePerPersonPerSession = pkg.Object.PricePerPersonPerSession,
                    PricePerPersonPerMonth = pkg.Object.PricePerPersonPerMonth
                }).Cast<object>().ToList(); // Chuyển kiểu vô danh thành object

            }
            else
            {
                return BadRequest("Invalid type.");
            }

            // Lấy danh sách PT
            var allTrainers = await _firebaseClient
                .Child("Trainers")
                .OnceAsync<Trainer>();

            trainers = allTrainers
                .Where(trainer =>
                    (type.Equals("Boxing", StringComparison.OrdinalIgnoreCase) && trainer.Object.IsTrainerBoxing) ||
                    (type.Equals("TrainerRental", StringComparison.OrdinalIgnoreCase) && trainer.Object.IsTrainerGym))
                .Select(trainer => new
                {
                    TrainerId = trainer.Key,
                    trainer.Object.Name,
                    trainer.Object.UserId
                }).Cast<object>().ToList(); // Chuyển kiểu vô danh thành object


            return Ok(new
            {
                Packages = packages,
                Trainers = trainers
            });
        }

        [HttpGet("trainers-by-option")]
        public async Task<IActionResult> GetTrainersByOptionId([FromQuery] string? boxingOptionId, [FromQuery] string? rentalOptionId)
        {
            if (string.IsNullOrEmpty(boxingOptionId) && string.IsNullOrEmpty(rentalOptionId))
            {
                return BadRequest("Either BoxingOptionId or RentalOptionId is required.");
            }

            List<object> trainers = new List<object>();

            if (!string.IsNullOrEmpty(boxingOptionId))
            {
                // Lấy thông tin gói boxing
                var boxingOptions = await _firebaseClient
                    .Child("BoxingOptions")
                    .OnceAsync<BoxingOption>();

                var matchingOption = boxingOptions.FirstOrDefault(option => option.Key == boxingOptionId);
                if (matchingOption == null)
                {
                    return NotFound("No matching boxing option found.");
                }

                var boxingPlans = await _firebaseClient
                    .Child("BoxingMembershipPlans")
                    .OnceAsync<BoxingMembershipPlan>();

                var matchingPlans = boxingPlans
                    .Where(plan => plan.Object.BoxingOptionId == boxingOptionId)
                    .ToList();

                foreach (var plan in matchingPlans)
                {
                    var trainer = await _firebaseClient
                        .Child("Trainers")
                        .Child(plan.Object.BoxingTrainerId)
                        .OnceSingleAsync<Trainer>();

                    // Lấy chuyên môn từ trường con
                    var specialization = await _firebaseClient
                        .Child("Trainers")
                        .Child(plan.Object.BoxingTrainerId)
                        .Child("specialization")
                        .OnceSingleAsync<string>();

                    if (trainer != null)
                    {
                        trainers.Add(new
                        {
                            TrainerId = plan.Object.BoxingTrainerId,
                            Name = trainer.Name,
                            UserId = trainer.UserId,
                            Specialization = specialization,
                            IsMonthlyPackage = matchingOption.Object.Months > 0,
                            MinSessions = matchingOption.Object.Months > 0 ? (int?)null : matchingOption.Object.Sessions,
                            MaxSessions = matchingOption.Object.Months > 0 ? (int?)null : matchingOption.Object.Sessions,
                            MemberCount = matchingOption.Object.MemberCount
                        });
                    }
                }
            }

            if (!string.IsNullOrEmpty(rentalOptionId))
            {
                // Lấy thông tin gói thuê huấn luyện viên
                var rentalOptions = await _firebaseClient
                    .Child("RentalOptions")
                    .OnceAsync<RentalOption>();

                var matchingOption = rentalOptions.FirstOrDefault(option => option.Key == rentalOptionId);
                if (matchingOption == null)
                {
                    return NotFound("No matching rental option found.");
                }

                var rentalPlans = await _firebaseClient
                    .Child("TrainerRentalPlans")
                    .OnceAsync<TrainerRentalPlan>();

                var matchingPlans = rentalPlans
                    .Where(plan => plan.Object.RentalOptionId == rentalOptionId)
                    .ToList();

                foreach (var plan in matchingPlans)
                {
                    var trainer = await _firebaseClient
                        .Child("Trainers")
                        .Child(plan.Object.TrainerId)
                        .OnceSingleAsync<Trainer>();

                    // Lấy chuyên môn từ trường con
                    var specialization = await _firebaseClient
                        .Child("Trainers")
                        .Child(plan.Object.TrainerId)
                        .Child("specialization")
                        .OnceSingleAsync<string>();

                    if (trainer != null)
                    {
                        trainers.Add(new
                        {
                            TrainerId = plan.Object.TrainerId,
                            Name = trainer.Name,
                            UserId = trainer.UserId,
                            Specialization = specialization,
                            IsMonthlyPackage = matchingOption.Object.PricePerPersonPerMonth > 0,
                            MinSessions = matchingOption.Object.PricePerPersonPerMonth > 0 ? (int?)null : matchingOption.Object.SessionCountMin,
                            MaxSessions = matchingOption.Object.PricePerPersonPerMonth > 0 ? (int?)null : matchingOption.Object.SessionCountMax,
                            MemberCount = matchingOption.Object.MemberCount
                        });
                    }
                }
            }

            if (trainers.Count == 0)
            {
                return NotFound("No trainers found for the given option ID(s).");
            }

            return Ok(trainers);
        }

    }

}
