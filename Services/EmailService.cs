using System.Net.Mail;
using System.Net;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Alpha_API.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Alpha_API.Wrapper.Interfaces;

namespace Alpha_API.Utils
{
	public class EmailService : IEmailService
	{
        private readonly IFirebaseAuth _firebaseAuth;
        private readonly HttpClient _httpClient;
        private readonly ISmtpClient _smtpClient;
        public EmailService(IFirebaseAuth firebaseAuth, ISmtpClient smtpClient)
		{
            _firebaseAuth = firebaseAuth;
            _httpClient = new HttpClient();
            _smtpClient = smtpClient;
        }

		public bool SendVerificationEmail(string email, string verificationLink, string password)
		{
			try
			{
				MailMessage mail = new MailMessage();
				mail.From = new MailAddress(_smtpClient.GetAddress());
				mail.To.Add(email);
				mail.Subject = "Verify your email";
				mail.Body = $"Your password is: {password}\nPlease verify your email by clicking on the link: {verificationLink}";

				//using (SmtpClient smtpServer = new SmtpClient(_smtpServer))
				//{
				//	smtpServer.Port = _port;
				//	smtpServer.Credentials = new NetworkCredential(_fromEmail, _password);
				//	smtpServer.EnableSsl = true;

				//	smtpServer.Send(mail);
				//}
				_smtpClient.Send(mail);

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending email: {ex.Message}");
				return false;
			}
		}
        public void SendEmailMessage(string email, string message, string subject)
        {
            try
            {
                // Gửi email thông báo cho người dùng
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(_smtpClient.GetAddress());
                mail.To.Add(email);
                mail.Subject = subject;
                mail.Body = message;

                //using (SmtpClient smtpServer = new SmtpClient(_smtpServer))
                //{
                //    smtpServer.Port = _port;
                //    smtpServer.Credentials = new NetworkCredential(_fromEmail, _password);
                //    smtpServer.EnableSsl = true;

                //    smtpServer.Send(mail);
                //}

				_smtpClient.Send(mail);
			}
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification email: {ex.Message}");
            }
        }
        public async Task<string> GetUserIdByEmail(string email)
		{
			try
			{
				// Use FirebaseAuth to get the user by email
				UserRecord userRecord = await _firebaseAuth.GetUserByEmailAsync(email);

				// Return the userId (Firebase UID)
				return userRecord.Uid;
			}
			catch (FirebaseAuthException ex)
			{
				// Handle cases where the user is not found
				if (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
				{
					Console.WriteLine($"User with the specified email not found.");
					return "";
				}

				Console.WriteLine(ex.Message);
				return "";
			}
		}

        public async Task<bool> SendPasswordResetEmail(string email)
        {
            try
            {
                // Gửi yêu cầu reset mật khẩu qua Firebase API
                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=AIzaSyDuRSqvyEO2Do04yj716Jq67e_iOcrvfNo";

                var data = new
                {
                    requestType = "PASSWORD_RESET",
                    email = email
                };

                var jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    // Nếu yêu cầu thành công, gửi email thông báo cho người dùng kiểm tra hộp thư
                    string notificationMessage = "Chúng tôi đã gửi một email yêu cầu reset mật khẩu đến địa chỉ email của bạn. Vui lòng kiểm tra hộp thư của bạn để thực hiện thay đổi mật khẩu.";
                    string subject = "Password Reset Request";
                    SendEmailMessage(email, notificationMessage, subject);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending password reset email: {ex.Message}");
                return false;
            }
        }

        // Phương thức hỗ trợ để lấy mã oobCode từ response
        private async Task<string> ExtractOobCode(HttpResponseMessage response)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<dynamic>(responseBody);
            return responseData?.oobCode?.ToString();
        }
    }

}
