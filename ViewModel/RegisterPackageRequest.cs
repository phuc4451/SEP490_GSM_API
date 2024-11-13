namespace Alpha_API.ViewModel
{
    public class RegisterPackageRequest
    {
        public List<string> Emails { get; set; }
        public string? BoxingMembershipPlanId { get; set; }
        public string? GymMembershipId { get; set; }
        public string? TrainerRentalPlanId { get; set; }
        public int? DurationMonths { get; set; }
        public int? Sessions { get; set; }
        public string? ScheduleId { get; set; }
        public bool QRPayment { get; set; }

    }
}
