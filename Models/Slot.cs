namespace Alpha_API.Models
{
	public class Slot
	{
		public string SlotId { get; set; } // PK
		public string ScheduleId { get; set; } // FK to Schedule
		public DateOnly Date { get; set; }
		public TimeOnly StartTime { get; set; }
		public TimeOnly EndTime { get; set; }
		public bool Attended { get; set; }
	}
}
