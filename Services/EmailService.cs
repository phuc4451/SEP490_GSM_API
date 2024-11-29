using System.Net.Mail;
using System.Net;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Alpha_API.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace Alpha_API.Utils
{
	public class EmailService
	{
		private readonly string _smtpServer;
		private readonly int _port;
		private readonly string _fromEmail;
		private readonly string _password;
        private readonly FirebaseAuth _firebaseAuth;
        private readonly HttpClient _httpClient;
        public EmailService(FirebaseAuth firebaseAuth)
		{
			// Configure SMTP settings
			_smtpServer = "smtp.gmail.com";
			_port = 587;
			_fromEmail = "phucvu159753@gmail.com";
			_password = "tlpmzxpedrnlkfnn";
            _firebaseAuth = firebaseAuth;
            _httpClient = new HttpClient();

        }

		public bool SendVerificationEmail(string email, string verificationLink)
		{
			try
			{
				MailMessage mail = new MailMessage();
				mail.From = new MailAddress(_fromEmail);
				mail.To.Add(email);
				mail.Subject = "Verify your email";
				mail.Body = $"Please verify your email by clicking on the link: {verificationLink}";

				using (SmtpClient smtpServer = new SmtpClient(_smtpServer))
				{
					smtpServer.Port = _port;
					smtpServer.Credentials = new NetworkCredential(_fromEmail, _password);
					smtpServer.EnableSsl = true;

					smtpServer.Send(mail);
				}

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error sending email: {ex.Message}");
				return false;
			}
		}
        private void SendEmailNotification(string email, string message)
        {
            try
            {
                // Gửi email thông báo cho người dùng
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(_fromEmail);
                mail.To.Add(email);
                mail.Subject = "Password Reset Request";
                mail.Body = message;

                using (SmtpClient smtpServer = new SmtpClient(_smtpServer))
                {
                    smtpServer.Port = _port;
                    smtpServer.Credentials = new NetworkCredential(_fromEmail, _password);
                    smtpServer.EnableSsl = true;

                    smtpServer.Send(mail);
                }
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

                    SendEmailNotification(email, notificationMessage);
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
