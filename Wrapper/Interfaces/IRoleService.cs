using Alpha_API.Models;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface IRoleService
	{
		Task<List<Role>> GetAllRoles();
		Task<string> GetRoleName(string roleId);
		Task<string> GetRoleOfUser(string userId);
	}
}
