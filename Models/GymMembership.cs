namespace Alpha_API.Models
{
	public class GymMembership
	{
		public string? GymMembershipId { get; set; } // PK
		public string Name { get; set; }
		public int? DurationMonths { get; set; }
		public int? SessionCount { get; set; }
		public decimal Price { get; set; }
	}
}
