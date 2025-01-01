using Alpha_API.Models;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface ISalaryService
	{
		Task<(IEnumerable<SalaryReport>, IEnumerable<SalaryReport>)> CalculateStaffSalaryAsync(string staffId);
		Task<(IEnumerable<SalaryReport>, IEnumerable<SalaryReport>)> CalculateTrainerSalaryAsync(string trainerId);
		Task CreateSalaryConfiguration(SalaryConfiguration salaryConfiguration);
		Task<IEnumerable<StaffShiftAssignment>> GetStaffAssignments();
		Task CompleteSalary(string reportId);
	}
}
