namespace Alpha_API.Models
{
	public class Payment
	{
		public string PaymentId { get; set; } // PK, matches code in Transaction table
		public string GymRegistrationId { get; set; } // FK, nullable
		public string TrainerRentalRegistrationId { get; set; } // FK, nullable
		public string BoxingRegistrationId { get; set; } // FK, nullable
		public string TransactionId { get; set; } // FK, nullable
		public decimal Amount { get; set; }
		public DateTime PaymentDate { get; set; }
		public string PaymentStatus { get; set; } // "Completed", "Pending", "Cancel"
		public string PaymentMethodId { get; set; } // FK to PaymentMethod
	}
}
