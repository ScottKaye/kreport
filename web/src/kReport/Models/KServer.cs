using System;
using MongoDB.Bson.Serialization.Attributes;

namespace kReport.Models
{
	public class kServer
    {
		[BsonElement("ip")]
		public string IP { get; set; }

		[BsonElement("name")]
		public string Name { get; set; }
	}
}
