namespace Alpha_API.Models
{
	public class SalaryReport
	{
		public string ReportId { get; set; }
		public string? StaffId { get; set; }
		public string? TrainerId { get; set; }
		public string AssignmentId { get; set; }
		public string? ShiftName { get; set; }
		public string ShiftId { get; set; }
		public string ConfigId { get; set; }
		public string FullName { get; set; }
		public int TotalShifts { get; set; }
		public int TotalSlots { get; set; }
		public int TotalPresent { get; set; }
		public int LateCount { get; set; }
		public int AbsenceCount { get; set; }
		public decimal TotalFines { get; set; }
		public decimal TotalShiftsSalary { get; set; }
		public decimal TotalSlotsSalary { get; set; }
		public decimal FinalSalary { get; set; }
		public bool IsBilled { get; set; }
		public DateTime FromDate { get; set; }
		public DateTime ToDate { get; set; }
	}

}
