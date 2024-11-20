using Alpha_API.Utils;
using System.Text.Json.Serialization;

namespace Alpha_API.Models
{
	public class Schedule
	{
		public string ScheduleId { get; set; } // PK
		public string UserIds { get; set; } // FK to User
		public string TrainerId { get; set; } // FK to Trainer
		[JsonConverter(typeof(DateOnlyJsonConverter))]
		public DateOnly FirstSlot { get; set; }
		[JsonConverter(typeof(DateOnlyJsonConverter))]
		public DateOnly LastSlot { get; set; }
		public int SlotCount { get; set; }
	}
}
