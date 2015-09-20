using kReport.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

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
		}
	}
}