using MongoDB.Bson;
using System;

namespace kReport.Models
{
	[Serializable]
	public class kUser
	{
		public ObjectId Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public bool Admin { get; set; }

		public string GetName()
		{
			return Name != null ? Name : Email;
		}
	}
}