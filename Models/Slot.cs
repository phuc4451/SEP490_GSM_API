using Alpha_API.Utils;
using System.Text.Json.Serialization;

namespace Alpha_API.Models
{
	public class Slot
	{
		public string SlotId { get; set; } // PK
		public string ScheduleId { get; set; } // FK to Schedule
		[JsonConverter(typeof(DateOnlyJsonConverter))]
		public DateOnly Date { get; set; }

		//[JsonConverter(typeof(TimeOnlyJsonConverter))]
		//public TimeOnly StartTime { get; set; }
		//[JsonConverter(typeof(TimeOnlyJsonConverter))]
		//public TimeOnly EndTime { get; set; }

		public string TimeSlotId { get; set; }
		public bool Attended { get; set; }
	}
}
