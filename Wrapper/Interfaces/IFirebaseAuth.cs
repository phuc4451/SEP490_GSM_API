using FirebaseAdmin.Auth;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface IFirebaseAuth
	{
		Task<UserRecord> CreateUserAsync(UserRecordArgs args);
		Task<UserRecord> GetUserByEmailAsync(string email);
		Task<FirebaseToken> VerifyIdTokenAsync(string idToken);
		Task<string> GenerateEmailVerificationLinkAsync(string email);
	}

}
