namespace Alpha_API.Models
{
	public class Transaction
	{
		public string TransactionId { get; set; } // PK from external payment gateway
		public string Gateway { get; set; }       // Bank name
		public DateTime TransactionDate { get; set; }
		public string AccountNumber { get; set; }
		public string Code { get; set; }          // Transfer code, to match with payment code
		public string Content { get; set; }
		public string TransferType { get; set; }  // "in" or "out"
		public decimal TransferAmount { get; set; }
	}

}
