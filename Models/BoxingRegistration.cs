namespace Alpha_API.Models
{
	public class BoxingRegistration
	{
		public string RegistrationId { get; set; } // PK
		public string BoxingMembershipPlanId { get; set; } // FK to BoxingMembership
		public string UserIds { get; set; } // FK to User
		public string ScheduleId { get; set; } // FK to Schedule
		public string PaymentId { get; set; } // FK to Payment
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public int SessionLeft { get; set; }
		public bool IsActive { get; set; }
	}
}
