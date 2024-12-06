namespace Alpha_API.ViewModel
{
    public class RegistrationSummary
    {
        public string UserId { get; set; }
        public string RegistrationId { get; set; }
        public string RegistrationType { get; set; } // "Boxing" hoặc "TrainerRental"
        public string Description { get; set; }
    }
}
