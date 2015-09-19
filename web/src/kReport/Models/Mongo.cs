using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Linq;
using System;
using System.Collections.Generic;

namespace kReport.Models
{
	public static class Mongo
	{
		internal static MongoClient Client { get; set; }
		internal static MongoDatabase Db { get; set; }

		internal static IQueryable<KRequest> GetAllRequests()
		{
			return Db.GetCollection<KRequest>("requests")
				.FindAll()
				.SetLimit(50)
				.SetSortOrder(SortBy.Ascending("_id"))
				.AsQueryable();
		}

		internal static KRequest GetRequestById(ObjectId id)
		{
			var query = Query<KRequest>.EQ(e => e.Id, id);
			return Db.GetCollection<KRequest>("requests").FindOne(query);
		}

		internal static void SaveRequest(KRequest request)
		{
			if (request != null)
			{
				request.Date = DateTime.Now;
				if (request.Reason != null)
				{
					request.Reason = new string(request.Reason.Trim().Where(char.IsLetterOrDigit).ToArray());
				}
				Db.GetCollection<Report>("requests").Save(request);
			}
		}

		internal static bool HasUsers()
		{
			return Db.GetCollection<kUser>("users").Count() > 0;
		}

		internal static kUser GetUserById(ObjectId id)
		{
			var query = Query<kUser>.EQ(u => u.Id, id);
			return Db.GetCollection<kUser>("users").FindOne(query);
		}

		internal static kUser GetUserById(string id)
		{
			return GetUserById(ObjectId.Parse(id));
		}

		internal static kUser GetUserByEmail(string email)
		{
			var query = Query<kUser>.EQ(u => u.Email, email);
			return Db.GetCollection<kUser>("users").FindOne(query);
		}

		internal static kUser GetUserByName(string name)
		{
			var query = Query<kUser>.EQ(u => u.Name, name);
			return Db.GetCollection<kUser>("users").FindOne(query);
		}

		internal static void AddUser(kUser user)
		{
			Db.GetCollection("users").Insert(user);
		}
	}
}