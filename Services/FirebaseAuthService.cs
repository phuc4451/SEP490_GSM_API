using Alpha_API.Wrapper.Interfaces;
using FirebaseAdmin.Auth;

namespace Alpha_API.Services
{
	public class FirebaseAuthService : IFirebaseAuth
	{
		public async Task<UserRecord> CreateUserAsync(UserRecordArgs args)
		{
			return await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
		}

		public async Task<UserRecord> GetUserByEmailAsync(string email)
		{
			return await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
		}

		public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
		{
			return await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
		}

		public async Task<string> GenerateEmailVerificationLinkAsync(string email)
		{
			return await FirebaseAuth.DefaultInstance.GenerateEmailVerificationLinkAsync(email);
		}
	}
}
