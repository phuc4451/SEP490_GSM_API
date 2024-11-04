namespace Alpha_API.Models
{
	public class Session
	{
		public string SessionId { get; set; } // PK
		public string ScheduleId { get; set; } // FK to Schedule
		public DateTime Date { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
	}
}
