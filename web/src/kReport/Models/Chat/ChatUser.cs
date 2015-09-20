using MongoDB.Bson;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace kReport.Models
{
	public class ChatUser
	{
		[JsonIgnore]
		public List<string> Connections = new List<string>();

		[JsonProperty("id")]
		public ObjectId Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("admin")]
		public bool Admin { get; set; }

		public ChatUser(kUser user)
		{
			Id = user.Id;
			Name = user.GetName();
			Admin = user.Admin;
		}
	}
}