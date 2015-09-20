using kReport.Infrastructure;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace kReport.Models
{
	public abstract class KRequest
	{
		private string _Reason;

		[BsonElement("server")]
		public Server Server { get; set; }

		[BsonElement("_id")]
		public ObjectId Id { get; set; }

		[BsonElement("sender")]
		public SteamUser Sender { get; set; }

		[BsonElement("date")]
		public DateTime Date { get; set; }

		[BsonElement("abbreviation")]
		public abstract string Abbreviation { get; }

		[BsonElement("reason")]
		public string Reason
		{
			get { return _Reason; }
			set
			{
				string temp = Helpers.Sanitize(value, 100);
				_Reason = temp;
			}
		}

		[BsonElement("done")]
		public bool Done { get; set; }
	}
}