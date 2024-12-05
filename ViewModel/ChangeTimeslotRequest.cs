namespace Alpha_API.ViewModel
{
    public class ChangeTimeslotRequest
    {
        public string userId { get; set; }
        public string trainerId { get; set; }
        public string oldSlotId { get; set; }
        public string newSlotId { get; set; }
        public string scheduleId { get; set; }
    }
}
