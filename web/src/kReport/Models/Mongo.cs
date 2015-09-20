using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Linq;
using System;
using System.Collections.Generic;
using kReport.Infrastructure;
using System.Dynamic;
using System.IO;

namespace kReport.Models
{
	public static class Mongo
	{
		internal static MongoClient Client { get; set; }
		internal static MongoDatabase Db { get; set; }

		public static bool IsConnected()
		{
			try
			{
				Client.GetServer().Ping();
                return true;
			}
			catch {
				return false;
			}
		}

		internal static IQueryable<kRequest> GetAllRequests()
		{
			return Db.GetCollection<kRequest>("requests")
				.FindAll()
				.SetLimit(50)
				.SetSortOrder(SortBy.Ascending("_id"))
				.AsQueryable();
		}

		internal static kRequest GetRequestById(ObjectId id)
		{
			var query = Query<kRequest>.EQ(e => e.Id, id);
			return Db.GetCollection<kRequest>("requests").FindOne(query);
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

		internal static dynamic GetSettings()
		{
			dynamic obj = new ExpandoObject();
			var enabledThemes = GetEnabledThemes();

			obj.Themes = Directory
				.EnumerateFiles(Startup.AppBasePath + "/wwwroot/Style/Themes", "*.css", SearchOption.TopDirectoryOnly)
				.Select(Path.GetFileNameWithoutExtension).Select(t => new
				{
					Name = t,
					Enabled = enabledThemes.Contains(t)
				});

			return obj;
		}

		internal static void SaveSettings(dynamic settings)
		{
			if (settings.Themes != null)
			{
				List<string> enabled = new List<string>();
				foreach (var theme in settings.Themes)
				{
					if ((bool)theme.Enabled)
					{
						enabled.Add((string)theme.Name);
					}
				}

				Db.GetCollection("settings").FindAndModify(new FindAndModifyArgs
				{
					Query = Query.EQ("_id", 1),
					Upsert = true,
					Update = Update.Set("themes", new BsonArray(enabled))
				});
            }
		}

		internal static void SaveRequest(kRequest request)
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
		internal static void IncrementRequestsToday(kRequest req)
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
				Db.GetCollection<kRequest>("requests").FindAndModify(new FindAndModifyArgs
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
				Db.GetCollection<kRequest>("requests").FindAndRemove(new FindAndRemoveArgs
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

		public static BsonArray GetEnabledThemes()
		{
			var settings = Db.GetCollection("settings").FindOne(Query.EQ("_id", 1));
			if (settings != null)
			{
				return settings.GetValue("themes").AsBsonArray;
			}
			else return null;
		}

		internal static void AddUser(kUser user)
		{
			Db.GetCollection("users").Insert(user);
		}
	}
}