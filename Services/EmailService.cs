using System.Net.Mail;
using System.Net;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Alpha_API.Models;

namespace Alpha_API.Utils
{
	public class EmailService
	{
		private readonly string _smtpServer;
		private readonly int _port;
		private readonly string _fromEmail;
		private readonly string _password;
		private readonly FirebaseAuth _firebaseAuth;

		public EmailService(FirebaseAuth firebaseAuth)
		{
			// Configure SMTP settings
			_smtpServer = "smtp.gmail.com";
			_port = 587;
			_fromEmail = "phucvu159753@gmail.com";
			_password = "tlpmzxpedrnlkfnn";
			_firebaseAuth = firebaseAuth;
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
	}

}
