namespace Alpha_API.Models
{
	public class FinanceStatistics
	{
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public decimal TotalIncome { get; set; }
		public decimal TotalExpense { get; set; }
		public int IncomeTransactionCount { get; set; }
		public int ExpenseTransactionCount { get; set; }
		public decimal NetBalance { get; set; }
	}
}
