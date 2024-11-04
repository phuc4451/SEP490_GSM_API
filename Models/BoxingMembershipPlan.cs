namespace Alpha_API.Models
{
	public class BoxingMembershipPlan
	{
		public string BoxingMembershipPlanId { get; set; } // PK
		public string BoxingTrainerId { get; set; }
		public string BoxingOptionId { get; set; } // FK to TrainerRentalPlans
	}
}
