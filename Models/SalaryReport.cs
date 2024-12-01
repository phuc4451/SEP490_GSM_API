namespace Alpha_API.Models
{
	public class SalaryReport
	{
		public string ReportId { get; set; }
		public string? StaffId { get; set; }
		public string? TrainerId { get; set; }
		public string FullName { get; set; }
		public int TotalShifts { get; set; }
		public int TotalSlots { get; set; }
		public int LateCount { get; set; }
		public int AbsenceCount { get; set; }
		public decimal TotalFines { get; set; }
		public decimal FinalSalary { get; set; }
		public bool IsBilled { get; set; }
		public DateTime FromDate { get; set; }
		public DateTime ToDate { get; set; }
	}

}
