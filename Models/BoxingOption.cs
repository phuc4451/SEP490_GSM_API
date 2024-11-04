namespace Alpha_API.Models
{
	public class BoxingOption
	{
		public string BoxingOptionId { get; set; } // PK
		public string Description { get; set; }
		public int Sessions { get; set; }
		public int Months { get; set; }
		public int MemberCount { get; set; }
		public decimal TotalPrice { get; set; }
	}
}
