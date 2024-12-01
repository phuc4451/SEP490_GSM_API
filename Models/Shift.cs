namespace Alpha_API.Models
{
	public class Shift
	{
		public string ShiftId { get; set; } // Unique identifier for the shift
		public string ShiftName { get; set; } // Name of the shift (e.g., "Morning Shift")
		public DateTime StartTime { get; set; } // Shift start time
		public DateTime EndTime { get; set; } // Shift end time
		public string Location { get; set; } // The location of the shift, if applicable
		public string ShiftType { get; set; } // (optional) Type of shift (e.g., "morning", "night")

		//// Recurrence type: "Daily", "Weekly", "Monthly", etc.
		//public string Recurrence { get; set; }

		//// If recurrence is daily, this defines the number of days until the shift repeats
		//public int? RecurrenceInterval { get; set; }
	}

}
