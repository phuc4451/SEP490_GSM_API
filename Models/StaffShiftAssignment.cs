namespace Alpha_API.Models
{
	public class StaffShiftAssignment
	{
		public string AssignmentId { get; set; } // Unique assignment ID
		public string StaffId { get; set; } // Reference to the Staff member
		public string ShiftId { get; set; } // Reference to the Shift
		public string ConfigurationId { get; set; }
		public DateTime AssignedDate { get; set; } // Date when this assignment occurs
		public DateTime EndDate { get; set; } // Date when this assignment ends
	}

}
