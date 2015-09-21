﻿using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Linq;
using System;
using System.Collections.Generic;
using kReport.Infrastructure;
using System.Dynamic;

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

		internal static int[] GetNumRequestsThisWeek()
		{
			int[] result = new int[7];
			DateTime sun = DateTime.Now.StartOfWeek(DayOfWeek.Sunday);
			List<IMongoQuery> queries = new List<IMongoQuery>();

			for (int i = 0; i < 7; ++i)
			{
				var query = Query.EQ("day", sun.AddDays(i).ToString("yyyy-MM-dd"));
				var day = Db.GetCollection("trackers").FindOne(query);
				if (day != null)
				{
					result[i] = day.GetValue("requests", 0).AsInt32;
				}
				else { result[i] = 0; }
			}

			return result;
		}

		internal static int[] GetNumRequestsThisYear()
		{
			int[] result = new int[12];

			for (int i = 0; i < 12; ++i)
			{
				string yearMonth = DateTime.Now.Year + "-" + (i + 1).ToString().PadLeft(2, '0');
				var query = Query.Matches("day", yearMonth);
				var month = Db.GetCollection("trackers").Find(query);
				if (month != null)
				{
					result[i] = month.Sum(d => (int)d.GetValue("requests", 0));
				}
				else { result[i] = 0; }
			}

			return result;
		}

		internal static dynamic GetServerStats()
		{
			var obj = new ExpandoObject() as IDictionary<string, object>;
			var servers = Db.GetCollection("servers").FindAll();

			foreach (var server in servers)
			{
				string name = (string)server.GetValue("name");
				obj.Add(name, (int)server.GetValue("requests"));
			}

			return obj;
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

		/// <summary>
		/// Used to count how many requests happen per day
		/// </summary>
		internal static void IncrementRequestsToday(KRequest req)
		{
			//Update requests for today
			Db.GetCollection("trackers").FindAndModify(new FindAndModifyArgs
			{
				Upsert = true,
				Query = Query.EQ("day", DateTime.Now.ToString("yyyy-MM-dd")),
				Update = Update.Inc("requests", 1)
			});

			//Update server requests
			if (req.Server.IP != null)
			{
				Db.GetCollection("servers").FindAndModify(new FindAndModifyArgs
				{
					Upsert = true,
					Query = Query.And(Query.EQ("ip", req.Server.IP), Query.EQ("name", req.Server.Name)),
					Update = Update.Inc("requests", 1)
				});
			}
		}

		internal static void Done(ObjectId[] ids, bool done)
		{
			foreach (ObjectId id in ids)
			{
				Db.GetCollection<KRequest>("requests").FindAndModify(new FindAndModifyArgs
				{
					Query = Query.EQ("_id", id),
					Update = Update.Set("done", done)
				});
			}
		}

		internal static void Delete(ObjectId[] ids)
		{
			foreach (ObjectId id in ids)
			{
				Db.GetCollection<KRequest>("requests").FindAndRemove(new FindAndRemoveArgs
				{
					Query = Query.EQ("_id", id)
				});
			}
		}

		public static bool HasUsers()
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
			var query = Query<kUser>.EQ(u => u.Name.ToLower(), name.ToLower());
			return Db.GetCollection<kUser>("users").FindOne(query);
		}

		internal static void AddUser(kUser user)
		{
			Db.GetCollection("users").Insert(user);
		}
	}
}