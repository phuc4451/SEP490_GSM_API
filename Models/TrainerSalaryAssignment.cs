namespace Alpha_API.Models
{
	public class TrainerSalaryAssignment
	{
		public string AssignmentId { get; set; }
		public string TrainerId { get; set; }
		public string ConfigurationId { get; set; }
		public DateTime AssignedDate { get; set; } // Date when this assignment occurs
		public DateTime EndDate { get; set; } // Date when this assignment ends
	}
}
