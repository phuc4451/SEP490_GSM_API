namespace Alpha_API.Models
{
	public class Feedback
	{
		public string FeedbackId { get; set; } 
		public string UserId { get; set; } 
		public string Message { get; set; } 
		public int Rating { get; set; }
		public DateTime SubmittedAt { get; set; } 
	}
}
