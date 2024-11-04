namespace Alpha_API.Models
{
	public class GymRegistration
	{
		public string RegistrationId { get; set; } // PK
		public string UserId { get; set; } // FK to User
		public string GymMembershipId { get; set; } // FK to GymMembership
		public string PaymentId { get; set; } // FK to Payment
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public int SessionLeft { get; set; }
		public bool IsActive { get; set; }
	}
}
