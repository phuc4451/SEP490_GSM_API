namespace Alpha_API.Wrapper.Interfaces
{
	public interface IGymMembershipCheckService
	{
		Task<bool> CheckGymMembership(string userId);
		Task<(bool,bool)> CheckGymMembershipEndDate(string userId, DateOnly endDate);
	}
}
