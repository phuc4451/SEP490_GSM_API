using Alpha_API.Models;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface IShiftService
	{
		Task CreateShiftAsync(Shift shift);
		Task<IEnumerable<SalaryConfiguration>> GetSalaryConfigAsync();
		Task<IEnumerable<Shift>> GetShiftAsync();
		Task AssignStaffToShiftAsync(StaffShiftAssignment shiftAssignment);
		Task<Shift> ShiftAtTimeAsync(string staffId, DateTime checkTime);
	}
}
