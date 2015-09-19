using System;
using MongoDB.Bson.Serialization.Attributes;

namespace kReport.Models
{
	public class Server
    {
		[BsonElement("ip")]
		public string IP { get; set; }

		[BsonElement("name")]
		public string Name { get; set; }
	}
}
