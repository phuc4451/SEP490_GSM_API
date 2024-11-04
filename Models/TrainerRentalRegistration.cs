namespace Alpha_API.Models
{
	public class TrainerRentalRegistration
	{
		public string RegistrationId { get; set; } // PK
		public string PlanId { get; set; } // FK to TrainerRentalPlan
		public string UserIds { get; set; } // FK to User
		public string ScheduleId { get; set; } // FK to Schedule
		public string PaymentId { get; set; } // FK to Payment
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public int SessionLeft { get; set; }
		public bool IsActive { get; set; }
	}
}
