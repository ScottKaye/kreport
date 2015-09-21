﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kReport.Models
{
	public class Report : kRequest
	{
		public override string Abbreviation
		{
			get { return "rep"; }
		}

		[BsonElement("target")]
		public SteamUser Target { get; set; }
	}
}