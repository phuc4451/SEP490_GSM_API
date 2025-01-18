using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Alpha_API.Models;
using ClosedXML.Excel;

namespace Alpha_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinanceController : ControllerBase
    {
        private readonly FirebaseClient _firebaseClient;

        public FinanceController()
        {
            const string FirebaseBaseUrl = "https://sgm-management-c98cd-default-rtdb.firebaseio.com/";
            _firebaseClient = new FirebaseClient(FirebaseBaseUrl);
        }

        // GET: api/Finance/transactions
        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            var transactions = await _firebaseClient
                .Child("Transactions")
                .OnceAsync<Transaction>();

            return transactions.Select(t => t.Object).ToList();
        }

        // GET: api/Finance/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<FinanceStatistics>> GetStatistics(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var transactions = await _firebaseClient
                .Child("Transactions")
                .OnceAsync<Transaction>();

            var filteredTransactions = transactions
                .Select(t => t.Object)
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .ToList();

            var totalIncome = filteredTransactions
                .Where(t => t.TransferType == "in")
                .Sum(t => t.TransferAmount);

            var totalExpense = filteredTransactions
                .Where(t => t.TransferType == "out")
                .Sum(t => t.TransferAmount);

            var statistics = new FinanceStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                IncomeTransactionCount = filteredTransactions.Count(t => t.TransferType == "in"),
                ExpenseTransactionCount = filteredTransactions.Count(t => t.TransferType == "out"),
                NetBalance = totalIncome - totalExpense
            };

            return statistics;
        }

        // GET: api/Finance/daily-statistics
        [HttpGet("daily-statistics")]
        public async Task<ActionResult<FinanceStatistics>> GetDailyStatistics([FromQuery] DateTime date)
        {
            var transactions = await _firebaseClient
                .Child("Transactions")
                .OnceAsync<Transaction>();

            var filteredTransactions = transactions
                .Select(t => t.Object)
                .Where(t => t.TransactionDate.Date == date.Date)
                .ToList();

            var totalIncome = filteredTransactions
                .Where(t => t.TransferType == "in")
                .Sum(t => t.TransferAmount);

            var totalExpense = filteredTransactions
                .Where(t => t.TransferType == "out")
                .Sum(t => t.TransferAmount);

            var statistics = new FinanceStatistics
            {
                StartDate = date,
                EndDate = date,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                IncomeTransactionCount = filteredTransactions.Count(t => t.TransferType == "in"),
                ExpenseTransactionCount = filteredTransactions.Count(t => t.TransferType == "out"),
                NetBalance = totalIncome - totalExpense
            };

            return statistics;
        }

        // Export transactions to Excel
        [HttpGet("export-transactions")]
        public async Task<IActionResult> ExportTransactionsToExcel([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var transactions = await _firebaseClient
                .Child("Transactions")
                .OnceAsync<Transaction>();

            var filteredTransactions = transactions
                .Select(t => t.Object)
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Transactions");

            // Create headers
            worksheet.Cell(1, 1).Value = "TransactionId";
            worksheet.Cell(1, 2).Value = "Gateway";
            worksheet.Cell(1, 3).Value = "TransactionDate";
            worksheet.Cell(1, 4).Value = "AccountNumber";
            worksheet.Cell(1, 5).Value = "Code";
            worksheet.Cell(1, 6).Value = "Content";
            worksheet.Cell(1, 7).Value = "TransferType";
            worksheet.Cell(1, 8).Value = "TransferAmount";

            // Populate rows
            for (int i = 0; i < filteredTransactions.Count; i++)
            {
                var transaction = filteredTransactions[i];
                worksheet.Cell(i + 2, 1).Value = transaction.TransactionId;
                worksheet.Cell(i + 2, 2).Value = transaction.Gateway;
                worksheet.Cell(i + 2, 3).Value = transaction.TransactionDate;
                worksheet.Cell(i + 2, 4).Value = transaction.AccountNumber;
                worksheet.Cell(i + 2, 5).Value = transaction.Code;
                worksheet.Cell(i + 2, 6).Value = transaction.Content;
                worksheet.Cell(i + 2, 7).Value = transaction.TransferType;
                worksheet.Cell(i + 2, 8).Value = transaction.TransferAmount;
            }

            // Prepare the file to return as a stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Transactions_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        //// POST: api/Finance/transaction
        //[HttpPost("transaction")]
        //public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        //{
        //	var result = await _firebaseClient
        //		.Child("Transactions")
        //		.PostAsync(transaction);

        //	transaction.TransactionId = result.Key;

        //	return CreatedAtAction(nameof(GetTransactions), new { id = transaction.TransactionId }, transaction);
        //}
        [HttpGet("sold-packages")]
        public async Task<ActionResult<Dictionary<string, int>>> GetSoldPackages([FromQuery] string month)
        {
            // Lấy năm hiện tại
            var today = DateTime.Today;
            DateTime firstDayOfYear = new DateTime(today.Year, 1, 1);
            DateTime lastDayOfYear = new DateTime(today.Year, 12, 31);

            // Truy vấn bảng Payments để tìm những paymentStatus là Completed
            var payments = await _firebaseClient
                .Child("Payments")
                .OnceAsync<Payment>();

            var completedPayments = payments
                .Select(p => p.Object)
                .Where(p => p.PaymentStatus == "Completed" && p.PaymentDate >= firstDayOfYear && p.PaymentDate <= lastDayOfYear)
                .ToList();

            // Dictionary để lưu số lượng gói bán được theo tên gói
            var soldPackagesCount = new Dictionary<string, int>();

            // Tạo danh sách gói cần kiểm tra
            var boxingRegistrations = _firebaseClient.Child("BoxingRegistrations").OnceAsync<BoxingRegistration>();
            var trainerRentalRegistrations = _firebaseClient.Child("TrainerRentalRegistrations").OnceAsync<TrainerRentalRegistration>();
            var gymRegistrations = _firebaseClient.Child("GymRegistrations").OnceAsync<GymRegistration>();

            var boxingMembershipPlans = _firebaseClient.Child("BoxingMembershipPlans").OnceAsync<BoxingMembershipPlan>();
            var rentalOptions = _firebaseClient.Child("RentalOptions").OnceAsync<RentalOption>();
            var gymMemberships = _firebaseClient.Child("GymMemberships").OnceAsync<GymMembership>();

            var boxingRegistrationDict = (await boxingRegistrations).ToDictionary(x => x.Key, x => x.Object);
            var trainerRentalRegistrationDict = (await trainerRentalRegistrations).ToDictionary(x => x.Key, x => x.Object);
            var gymRegistrationDict = (await gymRegistrations).ToDictionary(x => x.Key, x => x.Object);

            var boxingMembershipPlansDict = (await boxingMembershipPlans).ToDictionary(x => x.Key, x => x.Object);
            var rentalOptionsDict = (await rentalOptions).ToDictionary(x => x.Key, x => x.Object);
            var gymMembershipsDict = (await gymMemberships).ToDictionary(x => x.Key, x => x.Object);

            // Tính số lượng gói bán ra
            IEnumerable<Payment> selectedPayments = month == "AllMonths"
                ? completedPayments
                : completedPayments.Where(p => p.PaymentDate.ToString("MMMM") == month);

            foreach (var payment in selectedPayments)
            {
                string packageName = null;

                if (!string.IsNullOrEmpty(payment.BoxingRegistrationId))
                {
                    var boxingRegistration = boxingRegistrationDict.GetValueOrDefault(payment.BoxingRegistrationId);
                    if (boxingRegistration != null)
                    {
                        var boxingMembershipPlan = boxingMembershipPlansDict.GetValueOrDefault(boxingRegistration.BoxingMembershipPlanId);
                        if (boxingMembershipPlan != null)
                        {
                            var boxingOption = await _firebaseClient
                                .Child("BoxingOptions")
                                .Child(boxingMembershipPlan.BoxingOptionId)
                                .OnceSingleAsync<BoxingOption>();

                            packageName = boxingOption?.Description;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(payment.TrainerRentalRegistrationId))
                {
                    var trainerRentalRegistration = trainerRentalRegistrationDict.GetValueOrDefault(payment.TrainerRentalRegistrationId);
                    if (trainerRentalRegistration != null)
                    {
                        var trainerRentalPlan = await _firebaseClient
                            .Child("TrainerRentalPlans")
                            .Child(trainerRentalRegistration.PlanId)
                            .OnceSingleAsync<TrainerRentalPlan>();

                        if (trainerRentalPlan != null)
                        {
                            var rentalOption = rentalOptionsDict.GetValueOrDefault(trainerRentalPlan.RentalOptionId);
                            packageName = rentalOption?.Description;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(payment.GymRegistrationId))
                {
                    var gymRegistration = gymRegistrationDict.GetValueOrDefault(payment.GymRegistrationId);
                    if (gymRegistration != null)
                    {
                        var gymMembership = gymMembershipsDict.GetValueOrDefault(gymRegistration.GymMembershipId);
                        packageName = gymMembership?.Name;
                    }
                }

                // Cập nhật số lượng gói bán được
                if (!string.IsNullOrEmpty(packageName))
                {
                    if (soldPackagesCount.ContainsKey(packageName))
                    {
                        soldPackagesCount[packageName]++;
                    }
                    else
                    {
                        soldPackagesCount[packageName] = 1;
                    }
                }
            }

            return Ok(soldPackagesCount);
        }



        [HttpGet("registration-growth")]
        public async Task<ActionResult<object>> GetRegistrationGrowth()
        {
            // Lấy ngày đầu tháng hiện tại và tháng trước
            var today = DateTime.Today;
            var firstDayOfThisMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayOfLastMonth = firstDayOfThisMonth.AddMonths(-1);
            var lastDayOfLastMonth = firstDayOfThisMonth.AddDays(-1);

            // Truy vấn bảng GymRegistrations để lấy số lượng đăng ký trong tháng này, tháng trước và tổng số
            var registrations = await _firebaseClient
                .Child("GymRegistrations")
                .OnceAsync<GymRegistration>();

            // Lọc ra các đăng ký trong tháng này và tháng trước
            var registrationsThisMonth = registrations
                .Where(r => r.Object.StartDate >= firstDayOfThisMonth)
                .ToList();

            var registrationsLastMonth = registrations
                .Where(r => r.Object.StartDate >= firstDayOfLastMonth && r.Object.StartDate <= lastDayOfLastMonth)
                .ToList();

            // Tính tổng số đăng ký
            var totalRegistrationsCount = registrations.Count;

            // Số lượng đăng ký trong tháng này và tháng trước
            var registrationsThisMonthCount = registrationsThisMonth.Count;
            var registrationsLastMonthCount = registrationsLastMonth.Count;

            // Tính tỷ lệ tăng trưởng phần trăm
            double growthPercentage = 0;
            if (registrationsLastMonthCount > 0)
            {
                growthPercentage = ((double)(registrationsThisMonthCount - registrationsLastMonthCount) / registrationsLastMonthCount) * 100;
            }

            // Trả về kết quả
            var result = new
            {
                RegistrationsThisMonth = registrationsThisMonthCount,
                RegistrationsLastMonth = registrationsLastMonthCount,
                TotalRegistrations = totalRegistrationsCount,
                GrowthPercentage = growthPercentage
            };

            return Ok(result);
        }

        [HttpGet("revenue-growth")]
        public async Task<ActionResult<object>> GetRevenueGrowth()
        {
            // Lấy ngày đầu tháng hiện tại và tháng trước
            var today = DateTime.Today;
            var firstDayOfThisMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayOfLastMonth = firstDayOfThisMonth.AddMonths(-1);
            var lastDayOfLastMonth = firstDayOfThisMonth.AddDays(-1);

            // Truy vấn bảng Payments để lấy các giao dịch đã thanh toán
            var payments = await _firebaseClient
                .Child("Payments")
                .OnceAsync<Payment>();

            // Lọc các giao dịch thanh toán hoàn tất trong tháng này và tháng trước
            var paymentsThisMonth = payments
                .Where(p => p.Object.PaymentStatus == "Completed" && p.Object.PaymentDate >= firstDayOfThisMonth)
                .ToList();

            var paymentsLastMonth = payments
                .Where(p => p.Object.PaymentStatus == "Completed" && p.Object.PaymentDate >= firstDayOfLastMonth && p.Object.PaymentDate <= lastDayOfLastMonth)
                .ToList();

            // Tính tổng doanh thu trong tháng này và tháng trước
            var revenueThisMonth = paymentsThisMonth.Sum(p => p.Object.Amount);
            var revenueLastMonth = paymentsLastMonth.Sum(p => p.Object.Amount);

            // Tính tỷ lệ tăng trưởng doanh thu phần trăm, ép kiểu decimal sang double
            double growthPercentage = 0;
            if (revenueLastMonth > 0)
            {
                growthPercentage = (double)((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100;
            }

            // Trả về kết quả
            var result = new
            {
                RevenueThisMonth = revenueThisMonth,
                RevenueLastMonth = revenueLastMonth,
                TotalRevenue = payments.Where(p => p.Object.PaymentStatus == "Completed").Sum(p => p.Object.Amount),
                GrowthPercentage = growthPercentage
            };

            return Ok(result);
        }

        [HttpGet("revenue")]
        public async Task<ActionResult<object>> GetRevenue([FromQuery] string month)
        {
            // Lấy năm hiện tại
            var today = DateTime.Today;
            DateTime firstDayOfYear = new DateTime(today.Year, 1, 1);
            DateTime lastDayOfYear = new DateTime(today.Year, 12, 31);

            // Truy vấn bảng Payments để lấy các giao dịch thanh toán đã hoàn tất trong năm hiện tại
            var payments = await _firebaseClient
                .Child("Payments")
                .OnceAsync<Payment>();

            // Lọc các giao dịch thanh toán hoàn tất trong năm hiện tại
            var filteredPayments = payments
                .Where(p => p.Object.PaymentStatus == "Completed" && p.Object.PaymentDate >= firstDayOfYear && p.Object.PaymentDate <= lastDayOfYear)
                .Select(p => p.Object)
                .ToList();

            // Nếu chọn "Tổng doanh thu", trả về doanh thu cho tất cả các tháng trong năm
            if (month == "TotalRevenue")
            {
                var monthlyRevenue = new List<RevenuePerMonth>();

                // Tạo danh sách các tháng trong năm
                var months = Enumerable.Range(1, 12).Select(m => new DateTime(today.Year, m, 1)).ToList();

                // Tính doanh thu cho từng tháng
                monthlyRevenue = months.Select(m =>
                {
                    var monthlyRevenueAmount = filteredPayments
                        .Where(p => p.PaymentDate.Month == m.Month)
                        .Sum(p => p.Amount);  // Doanh thu tháng này

                    return new RevenuePerMonth
                    {
                        Month = m.ToString("MMMM yyyy"),  // Tên tháng (ví dụ: January 2024)
                        Revenue = monthlyRevenueAmount
                    };
                }).ToList();

                // Tính tổng doanh thu trong năm
                decimal totalRevenue = filteredPayments.Sum(p => p.Amount);

                return Ok(new
                {
                    TotalRevenue = totalRevenue,
                    MonthlyRevenue = monthlyRevenue
                });
            }

            // Nếu chọn tháng cụ thể (ví dụ: "January", "February" ...)
            var selectedMonth = DateTime.ParseExact(month, "MMMM", null);
            var selectedMonthRevenue = filteredPayments
                .Where(p => p.PaymentDate.Month == selectedMonth.Month)
                .Sum(p => p.Amount);

            return Ok(new
            {
                Month = selectedMonth.ToString("MMMM yyyy"),  // Tên tháng
                Revenue = selectedMonthRevenue
            });
        }

        [HttpGet("checkin")]
        public async Task<ActionResult<object>> GetCheckIn([FromQuery] string day)
        {
            var today = DateTime.Today;

            // 🔹 Lấy ngày Monday đầu tuần hiện tại (từ thứ Hai đến Chủ Nhật)
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(6);

            Console.WriteLine($"Start of Week: {startOfWeek:yyyy-MM-dd}");
            Console.WriteLine($"End of Week: {endOfWeek:yyyy-MM-dd}");

            var gymRegistrations = await _firebaseClient
                .Child("GymRegistrations")
                .OnceAsync<GymRegistration>();

            var activeUsers = gymRegistrations
                .Where(g => g.Object.IsActive)
                .Select(g => g.Object.UserId)
                .ToHashSet();

            var checkIns = await _firebaseClient
                .Child("CheckIns")
                .OnceAsync<CheckIn>();

            var filteredCheckIns = checkIns
                .Where(c => activeUsers.Contains(c.Object.UserId) &&
                            c.Object.Time != null &&
                            c.Object.Time.Value.Date >= startOfWeek.Date &&
                            c.Object.Time.Value.Date <= endOfWeek.Date)
                .Select(c => c.Object)
                .ToList();

            Console.WriteLine($"Filtered CheckIns Count: {filteredCheckIns.Count}");

            if (day == "AllDays")
            {
                var dailyCheckInCounts = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var date = startOfWeek.AddDays(i);
                        var checkInCount = filteredCheckIns
                            .Where(c => c.Time?.Date == date)
                            .GroupBy(c => c.UserId)
                            .Count();

                        return new CheckInPerDay
                        {
                            Day = date.ToString("dddd"),
                            CheckInCount = checkInCount
                        };
                    })
                    .ToList();

                return Ok(dailyCheckInCounts);
            }

            if (!DateTime.TryParseExact(day, "dddd", null, System.Globalization.DateTimeStyles.None, out var selectedDay))
            {
                return BadRequest(new { Message = "Invalid day format. Use full day name (e.g., 'Monday')." });
            }

            var selectedDate = startOfWeek.AddDays(((int)selectedDay.DayOfWeek + 6) % 7);

            var selectedDayCheckInCount = filteredCheckIns
                .Where(c => c.Time?.Date == selectedDate.Date)
                .GroupBy(c => c.UserId)
                .Count();

            return Ok(new
            {
                Day = selectedDate.ToString("dddd"),
                Date = selectedDate.ToString("yyyy-MM-dd"),
                CheckInCount = selectedDayCheckInCount
            });
        }

    }



    public class RevenuePerMonth
    {
        public string Month { get; set; }
        public decimal Revenue { get; set; }
    }
    public class CheckInPerDay
    {
        public string Day { get; set; }  // Tên ngày trong tuần (ví dụ: Monday, Tuesday, ...)
        public int CheckInCount { get; set; }  // Số lượt check-in của ngày đó
    }

}

