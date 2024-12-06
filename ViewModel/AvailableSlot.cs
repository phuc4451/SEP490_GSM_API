using Alpha_API.Utils;
using System.Text.Json.Serialization;

namespace Alpha_API.ViewModel
{
    public class AvailableSlot
    {
        public string SlotId { get; set; }

        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateOnly Date { get; set; }

        public string TimeSlotDescription { get; set; }
    }
}
