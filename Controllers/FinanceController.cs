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
        public async Task<ActionResult<Dictionary<string, int>>> GetSoldPackages(
           [FromQuery] DateTime startDate,
           [FromQuery] DateTime endDate)
        {
            // Truy vấn bảng Payments để tìm những paymentStatus là Completed
            var payments = await _firebaseClient
                .Child("Payments")
                .OnceAsync<Payment>();

            var completedPayments = payments
                .Select(p => p.Object)
                .Where(p => p.PaymentStatus == "Completed" && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .ToList();

            // Dictionary để lưu số lượng gói bán được theo tên gói
            var soldPackagesCount = new Dictionary<string, int>();

            // Duyệt qua tất cả các payment đã hoàn thành
            foreach (var payment in completedPayments)
            {
                // Kiểm tra loại gói đã mua từ các bảng BoxingRegistration, GymRegistrations, TrainerRentalRegistrations
                if (!string.IsNullOrEmpty(payment.BoxingRegistrationId))
                {
                    // Truy vấn BoxingMembershipPlanId từ bảng BoxingRegistration
                    var boxingRegistration = await _firebaseClient
                        .Child("BoxingRegistration")
                        .Child(payment.BoxingRegistrationId)
                        .OnceSingleAsync<BoxingRegistration>();

                    if (boxingRegistration != null)
                    {
                        // Truy vấn BoxingOptionId từ bảng BoxingMembershipPlans
                        var boxingMembershipPlan = await _firebaseClient
                            .Child("BoxingMembershipPlans")
                            .Child(boxingRegistration.BoxingMembershipPlanId)
                            .OnceSingleAsync<BoxingMembershipPlan>();

                        if (boxingMembershipPlan != null)
                        {
                            // Truy vấn BoxingOptions và lấy tên gói (description)
                            var boxingOptions = await _firebaseClient
                                .Child("BoxingOptions")
                                .OnceAsync<BoxingOption>();

                            var boxingOption = boxingOptions
                                .FirstOrDefault(opt => opt.Object.BoxingOptionId == boxingMembershipPlan.BoxingOptionId);

                            if (boxingOption != null)
                            {
                                var packageName = boxingOption.Object.Description; // Sửa ở đây

                                // Thêm hoặc cập nhật số lượng gói bán ra trong dictionary
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
                    }
                }
                else if (!string.IsNullOrEmpty(payment.GymRegistrationId))
                {
                    // Truy vấn GymMembershipId từ bảng GymRegistrations
                    var gymRegistration = await _firebaseClient
                        .Child("GymRegistrations")
                        .Child(payment.GymRegistrationId)
                        .OnceSingleAsync<GymRegistration>();

                    if (gymRegistration != null)
                    {
                        // Truy vấn GymMembershipId từ bảng GymMemberships
                        var gymMembership = await _firebaseClient
                            .Child("GymMemberships")
                            .Child(gymRegistration.GymMembershipId)
                            .OnceSingleAsync<GymMembership>();

                        if (gymMembership != null)
                        {
                            // Lấy tên gói (name) từ GymMemberships
                            var packageName = gymMembership.Name;

                            // Thêm hoặc cập nhật số lượng gói bán ra trong dictionary
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
                }
                else if (!string.IsNullOrEmpty(payment.TrainerRentalRegistrationId))
                {
                    // Truy vấn PlanId từ bảng TrainerRentalRegistrations
                    var trainerRentalRegistration = await _firebaseClient
                        .Child("TrainerRentalRegistrations")
                        .Child(payment.TrainerRentalRegistrationId)
                        .OnceSingleAsync<TrainerRentalRegistration>();

                    if (trainerRentalRegistration != null)
                    {
                        // Truy vấn RentalOptionId từ bảng TrainerRentalPlans
                        var trainerRentalPlan = await _firebaseClient
                            .Child("TrainerRentalPlans")
                            .Child(trainerRentalRegistration.PlanId)
                            .OnceSingleAsync<TrainerRentalPlan>();

                        if (trainerRentalPlan != null)
                        {
                            // Truy vấn RentalOptions và lấy description
                            var rentalOptions = await _firebaseClient
                                .Child("RentalOptions")
                                .OnceAsync<RentalOption>();

                            var rentalOption = rentalOptions
                                .FirstOrDefault(opt => opt.Object.RentalOptionId == trainerRentalPlan.RentalOptionId);

                            if (rentalOption != null)
                            {
                                var packageName = rentalOption.Object.Description; // Sửa ở đây

                                // Thêm hoặc cập nhật số lượng gói bán ra trong dictionary
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
            // Lấy ngày hiện tại và xác định tuần hiện tại
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);  // Chủ nhật của tuần này
            var endOfWeek = startOfWeek.AddDays(6);  // Thứ bảy của tuần này

            // Truy vấn bảng CheckIns để lấy dữ liệu
            var checkIns = await _firebaseClient
                .Child("CheckIns")
                .OnceAsync<CheckIn>();

            // Lọc các check-in trong tuần hiện tại
            var filteredCheckIns = checkIns
                .Where(c => c.Object.Time?.Date >= startOfWeek && c.Object.Time?.Date <= endOfWeek)
                .Select(c => c.Object)
                .ToList();

            // Nếu chọn "Tất cả các ngày", trả về số lượt check-in cho tất cả các ngày trong tuần
            if (day == "AllDays")
            {
                var dailyCheckInCounts = new List<CheckInPerDay>();

                // Tính số lượt check-in cho mỗi ngày trong tuần
                foreach (var i in Enumerable.Range(0, 7))
                {
                    var date = startOfWeek.AddDays(i);
                    var checkInCount = filteredCheckIns
                        .Count(c => c.Time?.Date == date);

                    dailyCheckInCounts.Add(new CheckInPerDay
                    {
                        Day = date.ToString("dddd"),  // Tên ngày (ví dụ: Monday, Tuesday, ...)
                        CheckInCount = checkInCount
                    });
                }

                return Ok(dailyCheckInCounts);
            }

            // Nếu chọn ngày cụ thể (ví dụ: "Monday", "Tuesday", ...)
            var selectedDay = DateTime.ParseExact(day, "dddd", null);
            var selectedDayCheckInCount = filteredCheckIns
                .Count(c => c.Time?.Date == selectedDay.Date);

            return Ok(new
            {
                Day = selectedDay.ToString("dddd"),  // Tên ngày
                CheckInCount = selectedDayCheckInCount
            });
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

}
