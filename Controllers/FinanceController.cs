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
	}

}
