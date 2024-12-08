using Alpha_API.Models;
using Alpha_API.Services;
using Alpha_API.ViewModel;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Alpha_API.Utils;
using System.Text;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Office2016.Excel;
using System.Security.Claims;
using ExcelDataReader.Log;
using System.Collections.Concurrent;
namespace Alpha_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private FirebaseClient _firebaseClient;
        private readonly FirebaseClientProvider _firebaseClientProvider;
        private readonly EmailService _emailService;
        private readonly TimeSlotService _timeSlotService;
        private readonly ScheduleService _scheduleService;
        private readonly ILogger<ScheduleController> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new DateOnlyJsonConverter(), new TimeOnlyJsonConverter() } // Thêm các converter tùy chỉnh nếu cần
        };

        public ScheduleController(
            FirebaseClient firebaseClient,
            ILogger<ScheduleController> logger,
            FirebaseClientProvider firebaseClientProvider,
            EmailService emailService,
            TimeSlotService timeSlotService,
            ScheduleService scheduleService)
        {
            _firebaseClient = firebaseClient;
            _logger = logger;
            _firebaseClientProvider = firebaseClientProvider;
            _emailService = emailService;
            _timeSlotService = timeSlotService;
            _scheduleService = scheduleService;
        }

        // GET: api/Schedule/Customer/{userId}
        [HttpGet("Customer/{userId}")]
        public async Task<ActionResult<IEnumerable<Schedule>>> GetCustomerSchedules(string userId)
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();
            var schedules = await _firebaseClient
            .Child("Schedules")
            .OnceAsync<Schedule>();

            var userSchedules = schedules.Where(sche =>
                sche.Object.UserIds != null &&
                sche.Object.UserIds.Split(',').Contains(userId)
                );

            if (!userSchedules.Any())
                return NotFound("No schedules found for this customer.");

            foreach (var schedule in userSchedules)
            {
                schedule.Object.ScheduleId = schedule.Key;
            }

            return userSchedules.Select(s => s.Object).ToList();
        }

        [HttpGet("Slot/Customer/{userId}/{year}/{month}")]
        public async Task<ActionResult<IEnumerable<Object>>> GetCustomerSchedulesByDate(string userId, int year, int month)
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

            // Lấy tất cả Schedules và Slots trong một lần truy vấn
            var schedulesTask = _firebaseClient.Child("Schedules").OnceAsync<Schedule>();
            var slotsTask = _firebaseClient.Child("Slots").OnceAsync<Slot>();
            var trainersTask = _firebaseClient.Child("Trainers").OnceAsync<Trainer>();

            await Task.WhenAll(schedulesTask, slotsTask, trainersTask);

            var schedules = schedulesTask.Result;
            var slots = slotsTask.Result;
            var trainers = trainersTask.Result;

            // Lọc danh sách schedules theo userId
            var userSchedules = schedules
                .Where(sche => sche.Object.UserIds != null && sche.Object.UserIds.Split(',').Contains(userId))
                .ToList();

            if (!userSchedules.Any())
                return NotFound("No schedules found for this customer.");

            // Lọc theo tháng và năm (dựa trên FirstSlot)
            var filteredSchedules = userSchedules
                .Where(sche =>
                {
                    // Convert FirstSlot và LastSlot sang DateTime để kiểm tra tháng và năm
                    var firstSlotDate = sche.Object.FirstSlot.ToDateTime(new TimeOnly(0, 0)); // Chuyển FirstSlot sang DateTime
                    var lastSlotDate = sche.Object.LastSlot.ToDateTime(new TimeOnly(0, 0));   // Chuyển LastSlot sang DateTime

                    // Kiểm tra nếu FirstSlot hoặc LastSlot thuộc tháng và năm cần lọc
                    return (firstSlotDate.Year == year && firstSlotDate.Month == month) ||
                           (lastSlotDate.Year == year && lastSlotDate.Month == month);
                })
                .ToList();

            if (!filteredSchedules.Any())
                return NotFound("No schedules found for the specified month and year.");

            // Xây dựng kết quả
            var result = slots
                .Join(filteredSchedules, slot => slot.Object.ScheduleId, schedule => schedule.Key, (slot, schedule) => new
                {
                    Slot = slot,
                    Schedule = schedule
                })
                .Select(item =>
                {
                    var trainer = trainers.FirstOrDefault(t => t.Key == item.Schedule.Object.TrainerId);
                    return new
                    {
                        trainerName = trainer?.Object.Name,
                        timeSlot = _timeSlotService.GetTimeSlot(item.Slot.Object.TimeSlotId),
                        date = item.Slot.Object.Date.ToString("yyyy-MM-dd") // Sử dụng ngày đúng từ Slot nếu cần
                    };
                })
                .ToList();

            return result;
        }


        [HttpGet("Slot/Trainer/{userId}/{year}/{month}")]
        public async Task<ActionResult<IEnumerable<Object>>> GetTrainerCustomerSchedulesByMonth(string userId, int year, int month)
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

            // Fetch the trainer directly
            var trainer = (await _firebaseClient
                .Child("Trainers")
                .OnceAsync<Trainer>())
                .FirstOrDefault(t => t.Object.UserId == userId);

            if (trainer == null)
                return NotFound("Trainer not found for the given userId.");

            var trainerId = trainer.Key;

            // Fetch schedules related to the trainer for the specific month and year
            var schedules = await _firebaseClient
                .Child("Schedules")
                .OrderBy("trainerId")
                .EqualTo(trainerId)
                .OnceAsync<Schedule>();

            if (!schedules.Any())
                return NotFound("No schedules found for this trainer.");

            // Filter schedules based on the provided month and year
            var filteredSchedules = schedules
                .Where(s =>
                    s.Object.FirstSlot.Year == year && s.Object.FirstSlot.Month == month)
                .ToList();

            if (!filteredSchedules.Any())
                return NotFound("No schedules found for this trainer in the selected month.");

            // Extract schedule IDs
            var scheduleIds = filteredSchedules.Select(s => s.Key).ToList();

            // Fetch all relevant slots in one call
            var slots = (await _firebaseClient
                .Child("Slots")
                .OnceAsync<Slot>())
                .Where(s => scheduleIds.Contains(s.Object.ScheduleId))
                .ToList();

            if (!slots.Any())
                return NotFound("No slots found for this trainer's schedules.");

            // Collect user IDs from schedules
            var userIds = filteredSchedules
                .SelectMany(s => s.Object.UserIds?.Split(',') ?? Array.Empty<string>())
                .Distinct()
                .ToList();

            // Fetch all necessary users in bulk
            var userDict = (await Task.WhenAll(userIds.Select(id =>
                _firebaseClient.Child("users").Child(id).OnceSingleAsync<User>()
            )))
            .Where(user => user != null)
            .ToDictionary(u => u.UserId);

            // Fetch all time slots in bulk
            var timeSlotIds = slots.Select(s => s.Object.TimeSlotId).Distinct();
            var timeSlotDict = timeSlotIds.ToDictionary(
                id => id,
                id => _timeSlotService.GetTimeSlot(id)
            );

            // Group schedules by date and time slot
            var groupedSchedules = slots
                .GroupBy(slot => slot.Object.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(
                    dateGroup => dateGroup.Key,
                    dateGroup => dateGroup
                        .GroupBy(slot => timeSlotDict.GetValueOrDefault(slot.Object.TimeSlotId)?.ToString() ?? "Unknown")
                        .ToDictionary(
                            timeSlotGroup => timeSlotGroup.Key,
                            timeSlotGroup => timeSlotGroup
                                .SelectMany(slot => filteredSchedules
                                    .FirstOrDefault(s => s.Key == slot.Object.ScheduleId)?
                                    .Object.UserIds?
                                    .Split(',')
                                    .Where(userId => userDict.ContainsKey(userId))
                                    .Select(userId => userDict[userId].Name) ?? Array.Empty<string>()
                                )
                                .ToList()
                        )
                );

            // Flatten the result into a list of objects
            var result = groupedSchedules
                .SelectMany(dateGroup => dateGroup.Value
                    .Select(timeSlotGroup => new
                    {
                        date = dateGroup.Key,
                        timeSlot = timeSlotGroup.Key,
                        customers = timeSlotGroup.Value
                    }))
                .ToList();

            if (!result.Any())
                return NotFound("No customers found for this trainer's schedules in the selected month.");

            return result;
        }



        // GET: api/Schedule/Trainer/{userId}
        [HttpGet("Trainer/{userId}")]
        public async Task<ActionResult<IEnumerable<Schedule>>> GetTrainerSchedules(string userId)
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();
            var schedules = await _firebaseClient
            .Child("Schedules")
            .OrderBy("trainerId")
            .EqualTo(userId)
            .OnceAsync<Schedule>();

            var userSchedules = schedules.Where(sche =>
                sche.Object.TrainerId != null &&
                sche.Object.TrainerId.Equals(userId)
                );

            if (!userSchedules.Any())
                return NotFound("No schedules found for this trainer.");

            foreach (var schedule in userSchedules)
            {
                schedule.Object.ScheduleId = schedule.Key;
            }

            return userSchedules.Select(s => s.Object).ToList();
        }

        // GET: api/Schedule/MySchedules
        [HttpGet("MySchedules")]
        public async Task<ActionResult<IEnumerable<Schedule>>> GetMySchedules()
        {
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

            // Retrieve the uid claim
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized("User not authenticated.");
            }

            var userId = userIdClaim.Value; // This is the Firebase UID

            var schedules = await _firebaseClient
            .Child("Schedules")
            .OnceAsync<Schedule>();
            //chua dc
            var userSchedules = schedules.Where(sche =>
                sche.Object.UserIds != null &&
                sche.Object.UserIds.Equals(userId)
                );

            if (!userSchedules.Any())
                return NotFound("No schedules found for this trainer.");

            foreach (var schedule in userSchedules)
            {
                schedule.Object.ScheduleId = schedule.Key;
            }

            return userSchedules.Select(s => s.Object).ToList();
        }

        [HttpGet("TimeSlots")]
        public async Task<ActionResult<IEnumerable<TimeSlot>>> GetAllTimeSlots()
        {
            // Lấy client Firebase
            _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

            // Lấy danh sách TimeSlots từ node "TimeSlots"
            var timeSlots = await _firebaseClient
                .Child("TimeSlots")
                .OnceAsync<TimeSlot>();

            // Kiểm tra nếu không có TimeSlot nào
            if (!timeSlots.Any())
            {
                return NotFound("No TimeSlots found.");
            }

            // Map dữ liệu từ Firebase thành danh sách TimeSlot và sắp xếp theo Time
            var result = timeSlots
                .Select(ts => new TimeSlot
                {
                    TimeSlotId = ts.Key,
                    Time = ts.Object.Time
                })
                .OrderBy(ts => TimeSpan.Parse(ts.Time.Split('-')[0])) // Sắp xếp theo giờ bắt đầu
                .ToList();

            return result;
        }


        [HttpGet("Slots/All")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllTrainersSchedules(DateTime? inputDate)
        {
            try
            {
                _firebaseClient = _firebaseClientProvider.GetFirebaseClient();

                // Định nghĩa các nhiệm vụ riêng biệt với kiểu trả về cụ thể
                var trainersTask = _firebaseClient.Child("Trainers").OnceAsync<Trainer>();
                var schedulesTask = _firebaseClient.Child("Schedules").OnceAsync<Schedule>();
                var slotsTask = _firebaseClient.Child("Slots").OnceAsync<Slot>();
                var usersTask = _firebaseClient.Child("users").OnceAsync<User>();
                var trainerRentalRegistrationsTask = _firebaseClient.Child("TrainerRentalRegistrations").OnceAsync<TrainerRentalRegistration>();
                var boxingRegistrationsTask = _firebaseClient.Child("BoxingRegistrations").OnceAsync<BoxingRegistration>();
                var trainerRentalPlansTask = _firebaseClient.Child("TrainerRentalPlans").OnceAsync<TrainerRentalPlan>();
                var boxingMembershipPlansTask = _firebaseClient.Child("BoxingMembershipPlans").OnceAsync<BoxingMembershipPlan>();
                var rentalOptionsTask = _firebaseClient.Child("RentalOptions").OnceAsync<RentalOption>();
                var boxingOptionsTask = _firebaseClient.Child("BoxingOptions").OnceAsync<BoxingOption>();

                // Chờ tất cả các nhiệm vụ hoàn thành đồng thời
                await Task.WhenAll(
                    trainersTask,
                    schedulesTask,
                    slotsTask,
                    usersTask,
                    trainerRentalRegistrationsTask,
                    boxingRegistrationsTask,
                    trainerRentalPlansTask,
                    boxingMembershipPlansTask,
                    rentalOptionsTask,
                    boxingOptionsTask
                );

                // Giải nén dữ liệu và chuyển thành Dictionary cho tra cứu nhanh
                var trainers = (await trainersTask).ToDictionary(t => t.Key, t => t.Object);
                var schedules = (await schedulesTask).ToList();
                var slots = (await slotsTask).ToList();
                var users = (await usersTask).ToDictionary(u => u.Key, u => u.Object);
                var trainerRentalRegistrations = (await trainerRentalRegistrationsTask).ToDictionary(tr => tr.Object.ScheduleId, tr => tr.Object);
                var boxingRegistrations = (await boxingRegistrationsTask).ToDictionary(br => br.Object.ScheduleId, br => br.Object);
                var trainerRentalPlans = (await trainerRentalPlansTask).ToDictionary(rp => rp.Key, rp => rp.Object);
                var boxingMembershipPlans = (await boxingMembershipPlansTask).ToDictionary(bp => bp.Key, bp => bp.Object);
                var rentalOptions = (await rentalOptionsTask).ToDictionary(ro => ro.Key, ro => ro.Object.Description);
                var boxingOptions = (await boxingOptionsTask).ToDictionary(bo => bo.Key, bo => bo.Object.Description);

                // Tạo Dictionary cho Schedules theo TrainerId
                var schedulesByTrainer = schedules.GroupBy(s => s.Object.TrainerId)
                                                  .ToDictionary(g => g.Key, g => g.ToList());

                // Tạo Dictionary cho Schedules theo ScheduleId
                var schedulesById = schedules.ToDictionary(s => s.Key, s => s.Object);

                // Lọc slots theo inputDate nếu có
                if (inputDate.HasValue)
                {
                    var targetDate = DateOnly.FromDateTime(inputDate.Value);
                    slots = slots.Where(slot => slot.Object.Date == targetDate).ToList();
                }

                // Tạo Dictionary cho Slots theo ScheduleId
                var slotsByScheduleId = slots.GroupBy(slot => slot.Object.ScheduleId)
                                             .ToDictionary(g => g.Key, g => g.ToList());

                var result = new List<object>();

                foreach (var trainer in trainers)
                {
                    var trainerName = trainer.Value.Name;
                    var trainerId = trainer.Key;

                    if (!schedulesByTrainer.TryGetValue(trainerId, out var trainerSchedules))
                    {
                        // Trainer không có schedule
                        result.Add(new
                        {
                            TrainerName = trainerName,
                            Schedules = "No slots found for this trainer"
                        });
                        continue;
                    }

                    var trainerSlots = new List<object>();

                    foreach (var schedule in trainerSchedules)
                    {
                        var scheduleId = schedule.Key;

                        if (!slotsByScheduleId.TryGetValue(scheduleId, out var scheduleSlots))
                        {
                            // Schedule không có slot
                            trainerSlots.Add(new
                            {
                                Date = schedule.Object.FirstSlot.ToString("yyyy-MM-dd"), // Sửa đổi tại đây
                                TimeSlot = "No scheduled time slots",
                                Customers = new List<object> { new { Name = "No customers" } },
                                RentalOption = "No rental option",
                                BoxingOption = "No boxing option"
                            });
                            continue;
                        }

                        foreach (var slot in scheduleSlots)
                        {
                            var timeSlot = _timeSlotService.GetTimeSlot(slot.Object.TimeSlotId) ?? "Unknown Time Slot";

                            // Lấy danh sách khách hàng
                            var customerIds = schedule.Object.UserIds?.Split(',').Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? new List<string>();
                            var customerDetails = customerIds
                                .Where(id => users.ContainsKey(id))
                                .Select(id => (object)new { Name = users[id].Name })
                                .ToList();

                            // Xác định loại đăng ký và kế hoạch
                            string rentalOptionDescription = "No rental option";
                            string boxingOptionDescription = "No boxing option";

                            if (trainerRentalRegistrations.TryGetValue(scheduleId, out var rentalRegistration))
                            {
                                var planId = rentalRegistration.PlanId;
                                if (trainerRentalPlans.TryGetValue(planId, out var rentalPlan))
                                {
                                    var rentalOptionId = rentalPlan.RentalOptionId;
                                    if (rentalOptions.TryGetValue(rentalOptionId, out var rentalDesc))
                                    {
                                        rentalOptionDescription = rentalDesc;
                                    }
                                }
                            }
                            else if (boxingRegistrations.TryGetValue(scheduleId, out var boxingRegistration))
                            {
                                var planId = boxingRegistration.BoxingMembershipPlanId;
                                if (boxingMembershipPlans.TryGetValue(planId, out var boxingPlan))
                                {
                                    var boxingOptionId = boxingPlan.BoxingOptionId;
                                    if (boxingOptions.TryGetValue(boxingOptionId, out var boxingDesc))
                                    {
                                        boxingOptionDescription = boxingDesc;
                                    }
                                }
                            }

                            // Thêm slot với Customers là List<object>
                            trainerSlots.Add(new
                            {
                                Date = slot.Object.Date.ToString("yyyy-MM-dd"),
                                TimeSlot = timeSlot,
                                Customers = customerDetails.Any()
                                    ? customerDetails
                                    : new List<object> { new { Message = "No customers" } },
                                RentalOption = rentalOptionDescription,
                                BoxingOption = boxingOptionDescription
                            });
                        }
                    }

                    // Nếu trainer không có slots nào sau khi lọc, thêm thông tin mặc định
                    if (!trainerSlots.Any())
                    {
                        trainerSlots.Add(new
                        {
                            Date = inputDate?.ToString("yyyy-MM-dd") ?? "No date specified",
                            TimeSlot = "No scheduled time slots",
                            Customers = new List<object> { new { Message = "No customers" } },
                            RentalOption = "No rental option",
                            BoxingOption = "No boxing option"
                        });
                    }

                    result.Add(new
                    {
                        TrainerName = trainerName,
                        Slots = trainerSlots
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log ngoại lệ (giả sử có dịch vụ logging)
                _logger.LogError(ex, "Error fetching trainer schedules");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }






        [HttpPost("changeTimeslot")]
        public async Task<IActionResult> ChangeTimeslot([FromBody] ChangeTimeslotRequest request)
        {
            try
            {
                // Kiểm tra sự tồn tại của scheduleId trong TrainerRentalRegistration hoặc BoxingRegistration
                var trainerRentalRegistration = await GetTrainerRentalRegistration(request.userId, request.scheduleId);
                var boxingRegistration = await GetBoxingRegistration(request.userId, request.scheduleId);

                if (trainerRentalRegistration == null && boxingRegistration == null)
                {
                    return BadRequest("Không tìm thấy đăng ký boxing hoặc trainer rental phù hợp.");
                }

                // Kiểm tra ngày bắt đầu của đăng ký
                var registrationStartDate = trainerRentalRegistration != null
                    ? DateOnly.FromDateTime(trainerRentalRegistration.StartDate)
                    : DateOnly.FromDateTime(boxingRegistration.StartDate);

                var todayDate = DateOnly.FromDateTime(DateTime.Now);

                if (registrationStartDate > todayDate)
                {
                    return BadRequest("Không thể thay đổi lịch trước khi đăng ký bắt đầu.");
                }

                // Lấy tất cả các slot liên quan đến scheduleId
                var slots = await _firebaseClient
                    .Child("Slots")
                    .OrderBy("scheduleId")
                    .EqualTo(request.scheduleId)
                    .OnceAsync<Slot>();

                if (slots == null || !slots.Any())
                {
                    return BadRequest("Không tìm thấy slot phù hợp cho lịch này.");
                }

                // Lọc các slot có ngày >= hôm nay và chưa được attended
                var futureSlots = slots.Where(s =>
                    s.Object.Date >= todayDate &&
                    !s.Object.Attended
                ).ToList();

                if (!futureSlots.Any())
                {
                    return BadRequest("Không có slot nào trong tương lai để thay đổi.");
                }

                // Kiểm tra sự sẵn sàng của Trainer cho các TimeSlotId mới
                var newTimeSlot = await _firebaseClient
                    .Child("TimeSlots")
                    .Child(request.newSlotId)
                    .OnceSingleAsync<TimeSlot>();

                if (newTimeSlot == null)
                {
                    return BadRequest("TimeSlot mới không hợp lệ.");
                }

                // Lấy thông tin trainer từ schedule
                var schedule = await _firebaseClient
                    .Child("Schedules")
                    .Child(request.scheduleId)
                    .OnceSingleAsync<Schedule>();

                if (schedule == null)
                {
                    return BadRequest("Schedule không tồn tại.");
                }

                var trainerId = schedule.TrainerId;

                // Chuẩn bị các slot cần kiểm tra sự sẵn sàng
                var slotsToCheck = futureSlots.Select(s => new Slot
                {
                    Date = s.Object.Date,
                    TimeSlotId = request.newSlotId
                }).ToList();

                // Kiểm tra sự sẵn sàng của Trainer
                bool isTrainerAvailable = await _scheduleService.CheckTrainerAvailability(trainerId, slotsToCheck);

                if (!isTrainerAvailable)
                {
                    return BadRequest("Trainer không có sẵn trong các TimeSlot mới cho các ngày được chọn.");
                }

                // Cập nhật TimeSlotId cho các slot tương lai
                foreach (var slot in futureSlots)
                {
                    slot.Object.TimeSlotId = request.newSlotId;

                    // Serialize với camelCase
                    var jsonString = JsonSerializer.Serialize(slot.Object, _jsonSerializerOptions);

                    // Cập nhật lại vào Firebase
                    await _firebaseClient
                        .Child("Slots")
                        .Child(slot.Key)
                        .PutAsync(jsonString);
                }

                return Ok(new
                {
                    message = "Lịch đã được thay đổi thành công cho các ngày tương lai.",
                    updatedSlotCount = futureSlots.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Đã xảy ra lỗi: {ex.Message}");
            }
        }



        // Hàm lấy thông tin TrainerRentalRegistration
        private async Task<TrainerRentalRegistration> GetTrainerRentalRegistration(string userId, string scheduleId)
        {
            var result = await _firebaseClient
                .Child("TrainerRentalRegistrations")
                .OnceAsync<TrainerRentalRegistration>();

            // Thêm log để kiểm tra dữ liệu trả về từ Firebase
            Console.WriteLine("Dữ liệu lấy được từ Firebase: ");
            foreach (var r in result)
            {
                Console.WriteLine($"UserIds: {r.Object.UserIds} - ScheduleId: {r.Object.ScheduleId}");

                // Tách userIds thành một mảng và kiểm tra xem userId có trong mảng không
                var userIdsArray = r.Object.UserIds.Split(',');  // Tách chuỗi userIds thành mảng

                // Kiểm tra xem userId có trong mảng không và scheduleId có trùng không
                if (userIdsArray.Contains(userId) && r.Object.ScheduleId == scheduleId)
                {
                    return r.Object;  // Trả về đối tượng nếu tìm thấy
                }
            }

            // Nếu không tìm thấy, trả về null
            return null;
        }


        // Hàm lấy thông tin BoxingRegistration
        private async Task<BoxingRegistration> GetBoxingRegistration(string userId, string scheduleId)
        {
            var result = await _firebaseClient
                .Child("BoxingRegistrations")
                .OnceAsync<BoxingRegistration>();

            // Thêm log để kiểm tra dữ liệu trả về từ Firebase
            Console.WriteLine("Dữ liệu từ BoxingRegistrations: ");
            foreach (var r in result)
            {
                Console.WriteLine($"UserIds: {r.Object.UserIds} - ScheduleId: {r.Object.ScheduleId}");
            }

            // Kiểm tra userId trong chuỗi UserIds
            return result
                .FirstOrDefault(r => r.Object.UserIds.ToString() == userId && r.Object.ScheduleId == scheduleId)?.Object;
        }




        // Hàm cập nhật timeslot cho người dùng
        private async Task<bool> UpdateTimeSlotForUser(string userId, string oldSlotId, string newSlotId, string scheduleId)
        {
            // Tìm tất cả các slot liên quan đến người dùng trong bảng "Slots"
            var slots = await _firebaseClient
                .Child("Slots")
                .OrderBy("scheduleId")
                .EqualTo(scheduleId)
                .OnceAsync<Slot>();

            var slotToUpdate = slots.FirstOrDefault(s => s.Object.TimeSlotId == oldSlotId);
            if (slotToUpdate == null)
                return false; // Không tìm thấy slot cũ

            // Cập nhật slotId mới cho slot
            slotToUpdate.Object.TimeSlotId = newSlotId;

            // Serialize với camelCase
            var jsonString = JsonSerializer.Serialize(slotToUpdate.Object, _jsonSerializerOptions);

            // Cập nhật lại vào Firebase
            await _firebaseClient
                .Child("Slots")
                .Child(slotToUpdate.Key)
                .PutAsync(jsonString);

            return true; // Trả về true nếu cập nhật thành công
        }
        [HttpPost("getRegistrationsByEmail")]
        public async Task<IActionResult> GetRegistrationsByEmail([FromBody] GetRegistrationsByEmailRequest request)
        {
            try
            {
                // Bước 1: Lấy userId từ email
                var user = await GetUserByEmail(request.Email);
                if (user == null)
                {
                    return BadRequest("Email is not valid.");
                }
                string userId = user.UserId;

                // Bước 2: Lấy danh sách BoxingRegistrations và TrainerRentalRegistrations
                var boxingRegistrationsTask = _firebaseClient
                    .Child("BoxingRegistrations")
                    .OnceAsync<BoxingRegistration>();

                var trainerRentalRegistrationsTask = _firebaseClient
                    .Child("TrainerRentalRegistrations")
                    .OnceAsync<TrainerRentalRegistration>();

                await Task.WhenAll(boxingRegistrationsTask, trainerRentalRegistrationsTask);

                var boxingRegistrations = boxingRegistrationsTask.Result
                    .Where(r => r.Object.UserIds.Split(',').Contains(userId))
                    .ToList();

                var trainerRentalRegistrations = trainerRentalRegistrationsTask.Result
                    .Where(r => r.Object.UserIds.Split(',').Contains(userId))
                    .ToList();

                // Kiểm tra nếu không có bất kỳ đăng ký nào
                if (!boxingRegistrations.Any() && !trainerRentalRegistrations.Any())
                {
                    return BadRequest("User does not have any registration.");
                }

                // Khởi tạo danh sách phản hồi an toàn cho luồng
                var responseList = new ConcurrentBag<RegistrationSummary>();

                // Bước 3: Xử lý BoxingRegistrations
                var boxingTasks = boxingRegistrations.Select(async boxingReg =>
                {
                    // Lấy BoxingMembershipPlan
                    var boxingMembershipPlan = await _firebaseClient
                        .Child("BoxingMembershipPlans")
                        .Child(boxingReg.Object.BoxingMembershipPlanId)
                        .OnceSingleAsync<BoxingMembershipPlan>();

                    // Lấy BoxingOption description
                    var boxingOption = await _firebaseClient
                        .Child("BoxingOptions")
                        .Child(boxingMembershipPlan.BoxingOptionId)
                        .OnceSingleAsync<BoxingOption>();

                    // Thêm vào danh sách phản hồi
                    responseList.Add(new RegistrationSummary
                    {
                        UserId = userId,  // Gửi UserId cùng với thông tin đăng ký
                        RegistrationId = boxingReg.Key,
                        RegistrationType = "Boxing",
                        Description = boxingOption.Description
                    });
                });

                // Bước 4: Xử lý TrainerRentalRegistrations
                var rentalTasks = trainerRentalRegistrations.Select(async rentalReg =>
                {
                    // Lấy TrainerRentalPlan
                    var trainerRentalPlan = await _firebaseClient
                        .Child("TrainerRentalPlans")
                        .Child(rentalReg.Object.PlanId)
                        .OnceSingleAsync<TrainerRentalPlan>();

                    // Lấy RentalOption description
                    var rentalOption = await _firebaseClient
                        .Child("RentalOptions")
                        .Child(trainerRentalPlan.RentalOptionId)
                        .OnceSingleAsync<RentalOption>();

                    // Thêm vào danh sách phản hồi
                    responseList.Add(new RegistrationSummary
                    {
                        UserId = userId,  // Gửi UserId cùng với thông tin đăng ký
                        RegistrationId = rentalReg.Key,
                        RegistrationType = "TrainerRental",
                        Description = rentalOption.Description
                    });
                });

                // Chờ tất cả các tác vụ hoàn thành
                await Task.WhenAll(boxingTasks.Concat(rentalTasks));

                // Chuyển ConcurrentBag thành List trước khi trả về
                var finalResponse = responseList.ToList();

                // Trả về danh sách phản hồi
                return Ok(finalResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Đã xảy ra lỗi: {ex.Message}");
            }
        }


        [HttpPost("getRegistrationDetails")]
        public async Task<IActionResult> GetRegistrationDetails([FromBody] GetRegistrationDetailsRequest request)
        {
            try
            {
                _logger.LogInformation($"Received GetRegistrationDetails request for RegistrationId: {request.RegistrationId}");

                if (string.IsNullOrWhiteSpace(request.RegistrationId))
                {
                    _logger.LogWarning("RegistrationId is null or empty.");
                    return BadRequest("RegistrationId không được để trống.");
                }

                // Lấy BoxingRegistration bằng key (RegistrationId)
                _logger.LogInformation("Fetching BoxingRegistration...");
                var boxingRegTask = _firebaseClient
                    .Child("BoxingRegistrations")
                    .Child(request.RegistrationId)
                    .OnceSingleAsync<BoxingRegistration>();

                // Lấy TrainerRentalRegistration bằng key (RegistrationId)
                _logger.LogInformation("Fetching TrainerRentalRegistration...");
                var trainerRentalRegTask = _firebaseClient
                    .Child("TrainerRentalRegistrations")
                    .Child(request.RegistrationId)
                    .OnceSingleAsync<TrainerRentalRegistration>();

                await Task.WhenAll(boxingRegTask, trainerRentalRegTask);

                var boxingReg = boxingRegTask.Result;
                var trainerRentalReg = trainerRentalRegTask.Result;

                _logger.LogInformation($"BoxingRegistration found: {boxingReg != null}");
                _logger.LogInformation($"TrainerRentalRegistration found: {trainerRentalReg != null}");

                if (boxingReg == null && trainerRentalReg == null)
                {
                    _logger.LogWarning($"No registration found for RegistrationId: {request.RegistrationId}");
                    return BadRequest("Không tìm thấy đăng ký.");
                }

                bool isBoxing = boxingReg != null;
                RegistrationDetails details = new RegistrationDetails
                {
                    RegistrationId = request.RegistrationId,
                    RegistrationType = isBoxing ? "Boxing" : "TrainerRental",
                    CurrentSlot = null // Chỉ lấy CurrentSlot, không lấy AvailableSlots
                };

                string scheduleId;
                string trainerId;

                if (isBoxing)
                {
                    _logger.LogInformation("Processing BoxingRegistration...");

                    // Lấy UserId từ BoxingRegistration
                    details.UserId = boxingReg.UserIds; // Giả sử có UserId trong BoxingRegistration

                    // Kiểm tra BoxingMembershipPlanId
                    _logger.LogInformation($"BoxingMembershipPlanId: {boxingReg.BoxingMembershipPlanId}");
                    if (string.IsNullOrWhiteSpace(boxingReg.BoxingMembershipPlanId))
                    {
                        _logger.LogError("BoxingMembershipPlanId is null or empty.");
                        return StatusCode(500, "BoxingMembershipPlanId không được để trống.");
                    }

                    // Lấy BoxingMembershipPlan
                    var boxingMembershipPlan = await _firebaseClient
                        .Child("BoxingMembershipPlans")
                        .Child(boxingReg.BoxingMembershipPlanId)
                        .OnceSingleAsync<BoxingMembershipPlan>();

                    if (boxingMembershipPlan == null)
                    {
                        _logger.LogError($"BoxingMembershipPlan with ID '{boxingReg.BoxingMembershipPlanId}' not found.");
                        return StatusCode(500, "BoxingMembershipPlan không tồn tại.");
                    }

                    // Lấy BoxingOptionId từ BoxingMembershipPlan
                    var boxingOptionId = boxingMembershipPlan.BoxingOptionId;
                    if (string.IsNullOrWhiteSpace(boxingOptionId))
                    {
                        _logger.LogError("BoxingOptionId is null or empty.");
                        return StatusCode(500, "BoxingOptionId không được để trống.");
                    }

                    // Lấy BoxingOption để lấy Description
                    var boxingOption = await _firebaseClient
                        .Child("BoxingOptions")
                        .Child(boxingOptionId)
                        .OnceSingleAsync<BoxingOption>();

                    if (boxingOption == null)
                    {
                        _logger.LogError($"BoxingOption with ID '{boxingOptionId}' not found.");
                        return StatusCode(500, "BoxingOption không tồn tại.");
                    }

                    details.Description = boxingOption.Description;

                    // Lấy ScheduleId
                    scheduleId = boxingReg.ScheduleId;
                    _logger.LogInformation($"ScheduleId: {scheduleId}");
                    if (string.IsNullOrWhiteSpace(scheduleId))
                    {
                        _logger.LogError("ScheduleId is null or empty.");
                        return StatusCode(500, "ScheduleId không được để trống.");
                    }

                    details.ScheduleId = scheduleId; // Gán ScheduleId vào details

                    // Lấy Schedule
                    var schedule = await _firebaseClient
                        .Child("Schedules")
                        .Child(scheduleId)
                        .OnceSingleAsync<Schedule>();

                    if (schedule == null)
                    {
                        _logger.LogError($"Schedule with ID '{scheduleId}' not found.");
                        return StatusCode(500, "Schedule không tồn tại.");
                    }

                    trainerId = schedule.TrainerId;
                    _logger.LogInformation($"TrainerId: {trainerId}");
                    if (string.IsNullOrWhiteSpace(trainerId))
                    {
                        _logger.LogError("TrainerId is null or empty.");
                        return StatusCode(500, "TrainerId không được để trống.");
                    }

                    // Lấy tên Trainer
                    var trainerName = await GetTrainerName(trainerId);
                    _logger.LogInformation($"Trainer Name: {trainerName}");
                    if (string.IsNullOrWhiteSpace(trainerName))
                    {
                        _logger.LogError($"TrainerName for TrainerId '{trainerId}' is null or empty.");
                        return StatusCode(500, "TrainerName không được để trống.");
                    }

                    details.TrainerId = trainerId;
                    details.TrainerName = trainerName;

                    // Lấy CurrentSlot
                    var currentSlot = await GetCurrentSlot(scheduleId);
                    details.CurrentSlot = currentSlot;

                    _logger.LogInformation($"Assigned Description: {details.Description}");
                    _logger.LogInformation($"Assigned TrainerName: {details.TrainerName}");
                    _logger.LogInformation($"Assigned CurrentSlot: {details.CurrentSlot != null}");
                }
                else if (trainerRentalReg != null)
                {
                    _logger.LogInformation("Processing TrainerRentalRegistration...");

                    // Lấy UserId từ TrainerRentalRegistration
                    details.UserId = trainerRentalReg.UserIds; // Giả sử có UserId trong TrainerRentalRegistration

                    // Kiểm tra TrainerRentalPlanId
                    _logger.LogInformation($"TrainerRentalPlanId: {trainerRentalReg.PlanId}");
                    if (string.IsNullOrWhiteSpace(trainerRentalReg.PlanId))
                    {
                        _logger.LogError("TrainerRentalPlanId is null or empty.");
                        return StatusCode(500, "TrainerRentalPlanId không được để trống.");
                    }

                    // Lấy TrainerRentalPlan
                    var trainerRentalPlan = await _firebaseClient
                        .Child("TrainerRentalPlans")
                        .Child(trainerRentalReg.PlanId)
                        .OnceSingleAsync<TrainerRentalPlan>();

                    if (trainerRentalPlan == null)
                    {
                        _logger.LogError($"TrainerRentalPlan with ID '{trainerRentalReg.PlanId}' not found.");
                        return StatusCode(500, "TrainerRentalPlan không tồn tại.");
                    }

                    // Lấy RentalOptionId từ TrainerRentalPlan
                    var rentalOptionId = trainerRentalPlan.RentalOptionId;
                    if (string.IsNullOrWhiteSpace(rentalOptionId))
                    {
                        _logger.LogError("RentalOptionId is null or empty.");
                        return StatusCode(500, "RentalOptionId không được để trống.");
                    }

                    // Lấy RentalOption để lấy Description
                    var rentalOption = await _firebaseClient
                        .Child("RentalOptions")
                        .Child(rentalOptionId)
                        .OnceSingleAsync<RentalOption>();

                    if (rentalOption == null)
                    {
                        _logger.LogError($"RentalOption with ID '{rentalOptionId}' not found.");
                        return StatusCode(500, "RentalOption không tồn tại.");
                    }

                    details.Description = rentalOption.Description;

                    // Lấy ScheduleId
                    scheduleId = trainerRentalReg.ScheduleId;
                    _logger.LogInformation($"ScheduleId: {scheduleId}");
                    if (string.IsNullOrWhiteSpace(scheduleId))
                    {
                        _logger.LogError("ScheduleId is null or empty.");
                        return StatusCode(500, "ScheduleId không được để trống.");
                    }

                    details.ScheduleId = scheduleId; // Gán ScheduleId vào details

                    // Lấy Schedule
                    var schedule = await _firebaseClient
                        .Child("Schedules")
                        .Child(scheduleId)
                        .OnceSingleAsync<Schedule>();

                    if (schedule == null)
                    {
                        _logger.LogError($"Schedule with ID '{scheduleId}' not found.");
                        return StatusCode(500, "Schedule không tồn tại.");
                    }

                    trainerId = schedule.TrainerId;
                    _logger.LogInformation($"TrainerId: {trainerId}");
                    if (string.IsNullOrWhiteSpace(trainerId))
                    {
                        _logger.LogError("TrainerId is null or empty.");
                        return StatusCode(500, "TrainerId không được để trống.");
                    }

                    // Lấy tên Trainer
                    var trainerName = await GetTrainerName(trainerId);
                    _logger.LogInformation($"Trainer Name: {trainerName}");
                    if (string.IsNullOrWhiteSpace(trainerName))
                    {
                        _logger.LogError($"TrainerName for TrainerId '{trainerId}' is null or empty.");
                        return StatusCode(500, "TrainerName không được để trống.");
                    }

                    details.TrainerId = trainerId;
                    details.TrainerName = trainerName;

                    // Lấy CurrentSlot
                    var currentSlot = await GetCurrentSlot(scheduleId);
                    details.CurrentSlot = currentSlot;

                    _logger.LogInformation($"Assigned Description: {details.Description}");
                    _logger.LogInformation($"Assigned TrainerName: {details.TrainerName}");
                    _logger.LogInformation($"Assigned CurrentSlot: {details.CurrentSlot != null}");
                }

                // Logging trước khi trả về
                var serialized = JsonSerializer.Serialize(details);
                _logger.LogInformation($"Serialized RegistrationDetails: {serialized}");

                _logger.LogInformation($"Returning RegistrationDetails for RegistrationId: {request.RegistrationId}");
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRegistrationDetails: {ex.Message}");
                return StatusCode(500, $"Đã xảy ra lỗi: {ex.Message}");
            }
        }





        // Hàm lấy tên Trainer từ trainerId
        private async Task<string> GetTrainerName(string trainerId)
        {
            if (string.IsNullOrWhiteSpace(trainerId))
            {
                _logger.LogWarning("TrainerId is null or empty.");
                return null;
            }

            _logger.LogInformation($"Fetching Trainer with TrainerId: {trainerId}");
            var trainer = await _firebaseClient
                .Child("Trainers")
                .Child(trainerId)
                .OnceSingleAsync<Trainer>();

            if (trainer == null)
            {
                _logger.LogWarning($"Trainer not found for TrainerId: {trainerId}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(trainer.Name))
            {
                _logger.LogWarning($"Trainer name is null or empty for TrainerId: {trainerId}");
                return null;
            }

            string trainerName = trainer.Name;  // Lấy trực tiếp tên từ Trainer
            _logger.LogInformation($"Trainer Name: {trainerName}");
            return trainerName;
        }


        // Hàm lấy CurrentSlot
        private async Task<CurrentSlot> GetCurrentSlot(string scheduleId)
        {
            _logger.LogInformation($"Fetching current slot for ScheduleId: {scheduleId}");

            try
            {
                // Lấy tất cả các Slot liên quan đến scheduleId (không cần kiểm tra Attended = true)
                var slots = await _firebaseClient
                    .Child("Slots")
                    .OrderBy("scheduleId")
                    .EqualTo(scheduleId)
                    .OnceAsync<Slot>();

                _logger.LogInformation($"Found {slots.Count} slots for ScheduleId: {scheduleId}");

                if (slots.Count == 0)
                {
                    _logger.LogWarning($"No slots found for ScheduleId: {scheduleId}");
                    return null;
                }

                // Lấy slot đầu tiên
                var currentSlotObj = slots.FirstOrDefault(); // Hoặc bạn có thể chọn theo điều kiện khác nếu cần

                if (currentSlotObj != null)
                {
                    _logger.LogInformation($"Found current slot with TimeSlotId: {currentSlotObj.Object.TimeSlotId}");

                    // Truy vấn TimeSlots để lấy thông tin thời gian
                    var timeSlot = await _firebaseClient
                        .Child("TimeSlots")
                        .Child(currentSlotObj.Object.TimeSlotId)
                        .OnceSingleAsync<TimeSlot>();

                    if (timeSlot != null)
                    {
                        _logger.LogInformation($"TimeSlot found: {timeSlot.Time}");
                        return new CurrentSlot
                        {
                            TimeSlotId = currentSlotObj.Object.TimeSlotId,
                            Time = timeSlot.Time
                        };
                    }
                    else
                    {
                        _logger.LogWarning($"No TimeSlot found for TimeSlotId: {currentSlotObj.Object.TimeSlotId}");
                    }
                }
                else
                {
                    _logger.LogWarning("No current slot found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCurrentSlot: {ex.Message}");
            }

            return null;
        }
        private async Task<User> GetUserByEmail(string email)
        {
            var users = await _firebaseClient
                .Child("users")
                .OrderBy("email")
                .EqualTo(email)
                .OnceAsync<User>();

            return users.FirstOrDefault()?.Object;
        }

    }

}

