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

			ChatUser user = Users.Values.Where(u => u.Connections.Contains(Context.ConnectionId)).Single();
			ChatMessage message = new ChatMessage
			{
				system = false,
				date = DateTime.Now.ToString("h:mm") + DateTime.Now.ToString("tt").ToLower(),
				message = content,
				author = user
			};
			Clients.All.sendMessage(message);
		}

		public override Task OnConnected()
		{
			var principal = (ClaimsPrincipal)Context.User;
			kUser user = Mongo.GetUserById(principal.FindFirst(ClaimTypes.NameIdentifier).Value);
			ChatUser chatUser;

			try
			{
				//Use existing user if already present in the Users ConcurrentDictionary
				if (!Users.TryGetValue(user.GetName(), out chatUser))
				{
					chatUser = new ChatUser(user);
				}

				chatUser.Connections.Add(Context.ConnectionId);

				int beforeCount = Users.Count;
				Users.AddOrUpdate(chatUser.Name, chatUser, (oldkey, oldvalue) => chatUser);

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

				//If the user closed their last connection
				if (user.Connections.Count == 0)
				{
					//Delay actually disconnecting the user by 5 seconds in case they rejoin quickly
					new System.Threading.Timer(obj =>
					{
						//If user still has no connections
						if (user.Connections.Count == 0)
						{
							ChatUser garbage;
							Users.TryRemove(user.Name, out garbage);

							SendSystemMessage(user.Name + " disconnected.");
							Clients.All.updateUsers(Users);
						}
					}, null, 5000, System.Threading.Timeout.Infinite);
				}
			}
			catch { }

			return base.OnDisconnected(stopCalled);
		}
	}
}