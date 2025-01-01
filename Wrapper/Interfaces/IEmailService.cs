namespace Alpha_API.Wrapper.Interfaces
{
    public interface IEmailService
    {
        bool SendVerificationEmail(string email, string verificationLink, string password);
        void SendEmailMessage(string email, string message, string subject);
        Task<string> GetUserIdByEmail(string email);
        Task<bool> SendPasswordResetEmail(string email);
        //Task<string> ExtractOobCode(HttpResponseMessage response);
	}
}
