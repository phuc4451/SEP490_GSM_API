using System.Net.Mail;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface ISmtpClient
	{
		void Send(MailMessage mail);
		string GetAddress();
	}
}
