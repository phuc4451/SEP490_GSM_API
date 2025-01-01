using Alpha_API.Models;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface IStaffService
	{
		Task<string> GetStaffName(string staffId);
		Task<IEnumerable<Staff>> GetStaffAsync();
	}
}
