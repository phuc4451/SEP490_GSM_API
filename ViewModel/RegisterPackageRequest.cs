namespace Alpha_API.ViewModel
{
    public class RegisterPackageRequest
    {
        public List<string> Emails { get; set; }
        public string? BoxingMembershipPlanId { get; set; }
        public string? GymMembershipId { get; set; }
        public string? TrainerRentalPlanId { get; set; }
        public bool QRPayment { get; set; }
		public int? Duration { get; set; }
		public string SelectedTimeSlot { get; set; }
		public bool IsMonWedFri { get; set; }

	}
}
