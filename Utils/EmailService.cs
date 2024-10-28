using System.Net.Mail;
using System.Net;

namespace Alpha_API.Utils
{
	public class EmailService
	{
		private readonly string _smtpServer;
		private readonly int _port;
		private readonly string _fromEmail;
		private readonly string _password;

		public EmailService()
		{
			// Configure SMTP settings
			_smtpServer = "smtp.gmail.com";
			_port = 587;
			_fromEmail = "phucvu159753@gmail.com";
			_password = "tlpmzxpedrnlkfnn";
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
	}

}
