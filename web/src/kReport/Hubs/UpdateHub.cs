using kReport.Infrastructure;
using kReport.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;

namespace kReport.Hubs
{
	[HubName("Update")]
	public class UpdateHub : Hub
	{
		public static void Test(IHubContext context)
		{
			context.Clients.All.test();
		}

		public static void NewRequest(IHubContext context, kRequest request)
		{
			context.Clients.All.newRequest(request);

			string type = request.GetType().Name;

			//Send all subscribed users an email notification
			var subscribers = Mongo.GetAllUsers().FindAll(u => u.NotificationSettings.ReceiveEmail);
			foreach (var user in subscribers)
			{
				TimeSpan localTime = DateTime.UtcNow.AddHours(user.TimeZoneOffset).TimeOfDay;
				if (user.NotificationSettings.EmailStart <= localTime && user.NotificationSettings.EmailEnd >= localTime)
				{
					Mail.Send(user.Email,
						"kReport: " + type,
						@"
						<h2>Hi " + user.GetName() + @",</h2> 
						<p>A <b>" + type.ToLowerInvariant() + "</b> request has been made from <b>" + request.Server.Name + @".</b></p>
						<p>Have a nice day!</p>
						<p style='font-size:0.8em'>You can change your email settings any time from the Settings page on kReport.  If you did not sign up for these emails, please reply with your concern!</p>");
				}
			}
		}
	}
}