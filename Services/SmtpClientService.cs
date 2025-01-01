using System.Net.Mail;
using System.Net;
using Alpha_API.Wrapper.Interfaces;

namespace Alpha_API.Services
{
	public class SmtpClientService : ISmtpClient
	{
		private readonly SmtpClient _smtpClient;
		private readonly string _fromEmail;

		public SmtpClientService(IConfiguration configuration)
		{
			_smtpClient = new SmtpClient(configuration["Smtp:Server"])
			{
				Port = int.Parse(configuration["Smtp:Port"]),
				Credentials = new NetworkCredential(
					configuration["Smtp:Username"],
					configuration["Smtp:Password"]),
				EnableSsl = bool.Parse(configuration["Smtp:EnableSsl"])
			};

			_fromEmail = configuration["Smtp:Username"];
		}

		public string GetAddress()
		{
			return _fromEmail;
		}

		public void Send(MailMessage mail)
		{
			_smtpClient.Send(mail);
		}
	}
}
