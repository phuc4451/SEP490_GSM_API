using Alpha_API.Models;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface ITrainerService
	{
		Task<string> AddTrainerAsync(Trainer trainer);
		Task<List<Trainer>> GetAllTrainersAsync();
		Task<Trainer> GetTrainerByIdAsync(string trainerId);
		Task<bool> UpdateTrainerAsync(string id, Trainer trainer);
		Task<bool> DeleteTrainerAsync(string trainerId);
		Task<Slot> SlotAtTimeAsync(string trainerId, DateTime time);
		Task<string> GetTrainerName(string trainerId);
		Task AssignSalaryConfigToTrainerAsync(TrainerSalaryAssignment assignment);
	}
}
