namespace Alpha_API.Models
{
	public class RentalOption
	{
		public string RentalOptionId { get; set; } // PK
		public string Description { get; set; }
		public int SessionCountMin { get; set; }
		public int SessionCountMax { get; set; }
		public int MemberCount { get; set; }
		public decimal PricePerPersonPerSession { get; set; }
		public decimal PricePerPersonPerMonth { get; set; }
	}
}
