namespace Alpha_API.Models
{
	public class Trainer
	{
		public string TrainerId { get; set; } // PK
		public string Name { get; set; }
		public string TrainerTypeId { get; set; } // FK to TrainerType
		public bool IsSpecial { get; set; }
	}
}
