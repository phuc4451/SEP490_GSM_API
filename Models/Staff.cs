namespace Alpha_API.Models
{
	public class Staff
	{
		public string StaffId { get; set; } // Unique identifier for staff
		public string FullName { get; set; } // Full name of the staff member
		public string Position { get; set; } // Position of the staff (e.g., "Trainer", "Admin")
		public string UserId { get; set; }
	}

}
