namespace Alpha_API.Models
{
	public class Schedule
	{
		public string ScheduleId { get; set; } // PK
		public string UserId { get; set; } // FK to User
		public string TrainerId { get; set; } // FK to Trainer
		public DateTime FirstSession { get; set; }
		public DateTime LastSession { get; set; }
		public int SessionCount { get; set; }
	}
}
