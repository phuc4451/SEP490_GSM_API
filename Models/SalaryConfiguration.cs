namespace Alpha_API.Models
{
	public class SalaryConfiguration
	{
		public string ConfigurationId { get; set; } // Staff or Trainer ID
		public decimal BaseSalary { get; set; } // Fixed monthly salary
		public decimal PerShiftSalary { get; set; } // Payment per shift (if applicable)
		public decimal PerSlotSalary { get; set; } // Payment per shift (if applicable)
		public decimal FinePerLate { get; set; } // Fine for being late
		public decimal FinePerAbsence { get; set; } // Fine for missing a shift or schedule
	}
}
