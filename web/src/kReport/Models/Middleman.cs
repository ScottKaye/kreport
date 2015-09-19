using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kReport.Models
{
	public class Middleman : KRequest
	{
		public override string Abbreviation
		{
			get { return "mm"; }
		}
	}
}
