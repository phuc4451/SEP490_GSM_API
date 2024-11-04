namespace Alpha_API.Models
{
	public class TrainerRentalPlan
	{
		public string TrainerRentalPlanId { get; set; } // PK
		public string TrainerId { get; set; } // FK to Trainer
		public string RentalOptionId { get; set; } // FK to RentalOption
	}
}
