namespace Alpha_API.Models
{
	public class Trainer
	{
		public string TrainerId { get; set; } // PK
		public string UserId { get; set; }
		public string Name { get; set; }
		public bool IsTrainerGym { get; set; }
		public bool IsTrainerBoxing { get; set; }
	}
}
