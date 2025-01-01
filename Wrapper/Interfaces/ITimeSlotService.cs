namespace Alpha_API.Wrapper.Interfaces
{
	public interface ITimeSlotService
	{
		Task LoadTimeSlotsAsync();
		string GetTimeSlot(string timeSlotId);
		Dictionary<string, string> GetAllTimeSlots();
	}
}
