using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace kReport.Infrastructure
{
	public static class Mail
	{
		//https://www.google.com/settings/security/lesssecureapps
		public static void Send(string to, string subject, string message)
		{
			string user = Startup.Configuration.GetSection("kreport:email:user").Value;
			string pass = Startup.Configuration.GetSection("kreport:email:pass").Value;

			if (user == null || pass == null) throw new UnauthorizedAccessException("Incorrect credentials supplied for GMail.");

			MailMessage msg = new MailMessage(user, to);
			msg.IsBodyHtml = true;
			msg.Subject = subject;
			msg.Body = message;

			SmtpClient client = new SmtpClient();
			client.Host = "smtp.gmail.com";
			client.Port = 587;
			client.Credentials = new NetworkCredential(user, pass);
			client.EnableSsl = true;
			client.Send(msg);
		}
	}
}
