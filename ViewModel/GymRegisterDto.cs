namespace Alpha_API.ViewModel
{
	public class GymRegisterDto
	{
		public string UserId { get; set; }
		public string UserName { get; set; }
		public string GymMembershipId { get; set; } // FK to GymMembership
		public string PaymentStatus { get; set; } // FK to Payment
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public int SessionLeft { get; set; }
		public bool IsActive { get; set; }
	}
}
