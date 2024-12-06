namespace Alpha_API.ViewModel
{
    public class RegistrationDetails
    {
        public string RegistrationId { get; set; }
        public string RegistrationType { get; set; }
        public string Description { get; set; }
        public string TrainerId { get; set; }
        public string TrainerName { get; set; }
        public string ScheduleId { get; set; }
        public CurrentSlot CurrentSlot { get; set; }
        public string UserId { get; set; }
    }
}
