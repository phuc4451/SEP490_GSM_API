using Alpha_API.Models;

namespace Alpha_API.ViewModel
{
	public class RegisterScheduleRequest
	{
		public List<string> Emails { get; set; }
		//public List<Slot> Slots { get; set; }
		public int? Duration { get; set; }
		public string? BoxingMembershipPlanId { get; set; }
		public string? TrainerRentalPlanId { get; set; }
		public string SelectedTimeSlot { get; set; }
		public bool IsMonWedFri { get; set; }
	}
}
