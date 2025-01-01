using Alpha_API.Models;
using Alpha_API.ViewModel;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface IScheduleService
	{
		Task<(string, DateOnly)> CreateSchedule(RegisterScheduleRequest request, string userIdsString);
		Task<bool> CheckTrainerAvailability(string trainerId, List<Slot> slots);
	}
}
