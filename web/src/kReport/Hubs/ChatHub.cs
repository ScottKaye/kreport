using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace kReport.Models
{
	[HubName("Chat")]
	public class ChatHub : Hub
	{
		private static readonly ConcurrentDictionary<string, ChatUser> Users = new ConcurrentDictionary<string, ChatUser>();

		public void SendSystemMessage(string content)
		{
			Clients.All.sendMessage(new ChatMessage
			{
				author = null,
				system = true,
				message = content
			});
		}

		public void SendMessage(string content)
		{
			//TODO: Encode content to prevent XSS
			content = content.Trim();

			if (content.Length == 0) return;
			if (content.Length > 200)
			{
				content = content.Substring(0, 200);
			}

			ChatUser user = Users.Values.Where(u => u.Connections.Contains(Context.ConnectionId)).SingleOrDefault();
			if (user != null)
			{
				ChatMessage message = new ChatMessage
				{
					system = false,
					date = DateTime.Now.ToString("h:mm") + DateTime.Now.ToString("tt").ToLower(),
					message = content,
					author = user
				};
				Clients.All.sendMessage(message);
			}
		}

		public override Task OnConnected()
		{
			try
			{
				var principal = (ClaimsPrincipal)Context.User;
				var id = principal.FindFirst(ClaimTypes.NameIdentifier).Value;
				kUser user = Mongo.GetUserById(id);
				ChatUser chatUser;

				//Use existing user if already present in the Users ConcurrentDictionary
				if (!Users.TryGetValue(id, out chatUser))
				{
					chatUser = new ChatUser(user);
				}

				//Update name
				chatUser.Name = user.GetName();

				chatUser.Connections.Add(Context.ConnectionId);

				int beforeCount = Users.Count;
				Users.AddOrUpdate(id, chatUser, (oldkey, oldvalue) => chatUser);

				//Only show a connection message if the user is newly joining
				//Do not show a connection message if they just duplicated the tab/opened in a new page
				if (Users.Count > beforeCount)
				{
					SendSystemMessage(user.GetName() + " connected.");
				}

				Clients.All.updateUsers(Users);
			}
			catch { }

			return base.OnConnected();
		}

		public override Task OnDisconnected(bool stopCalled)
		{
			try
			{
				ChatUser user = Users.Values.Where(u => u.Connections.Contains(Context.ConnectionId)).Single();
				user.Connections.Remove(Context.ConnectionId);

				if (user.Connections.Count == 0)
				{
					ChatUser chatout;
					SendSystemMessage(user.Name + " disconnected.");
					Users.TryRemove(user.Id.ToString(), out chatout);
					Clients.All.updateUsers(Users);
				}
			}
			catch { }

			return base.OnDisconnected(stopCalled);
		}
	}
}