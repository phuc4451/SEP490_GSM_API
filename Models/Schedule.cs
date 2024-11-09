namespace Alpha_API.Models
{
	public class Schedule
	{
		public string ScheduleId { get; set; } // PK
		public string UserIds { get; set; } // FK to User
		public string TrainerId { get; set; } // FK to Trainer
		public DateOnly FirstSession { get; set; }
		public DateOnly LastSession { get; set; }
		public int SessionCount { get; set; }
	}
}
