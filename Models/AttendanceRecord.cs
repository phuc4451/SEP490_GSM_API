namespace Alpha_API.Models
{
	public class AttendanceRecord
	{
		public string StaffId { get; set; } // Staff or Trainer ID
		public string TrainerId { get; set; } // Staff or Trainer ID
		public DateTime Time { get; set; }
		public bool IsPresent { get; set; }
		public bool IsLate { get; set; }
	}
}
