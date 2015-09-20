using kReport.Hubs;
using kReport.Infrastructure;
using kReport.Models;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace kReport.Controllers
{
	//Allow API requests to store a Key somewhere for verification
	//TODO: Do this with interfaces
	public class ApiReport
	{
		public Report KRequest { get; set; }
		public string Key { get; set; }
	}

	public class ApiMiddleman
	{
		public Middleman KRequest { get; set; }
		public string Key { get; set; }
	}

	public class LoginUserInfo
	{
		public string UsernameOrEmail { get; set; }
		public string Password { get; set; }
	}

	public class FirstUserInfo
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public string ConfirmPassword { get; set; }
	}

	[Route("[controller]/[action]")]
	public class kController : Controller
	{
		private IConnectionManager _connectionManager;
		private IHubContext updateContext;

		[FromServices]
		public IConnectionManager ConnectionManager
		{
			get { return _connectionManager; }
			set
			{
				_connectionManager = value;
				updateContext = _connectionManager.GetHubContext<UpdateHub>();
			}
		}

		[HttpGet]
		public IEnumerable<kRequest> GetAllRequests()
		{
			return Mongo.GetAllRequests().AsEnumerable();
		}

		public kRequest GetRequestById(string id)
		{
			ObjectId objid;
			if (ObjectId.TryParse(id, out objid))
			{
				return Mongo.GetRequestById(objid);
			}
			return null;
		}

		[HttpGet]
		public int[] GetNumRequestsThisWeek()
		{
			return Mongo.GetNumRequestsThisWeek();
		}

		[HttpGet]
		public int[] GetNumRequestsThisYear()
		{
			return Mongo.GetNumRequestsThisYear();
		}

		[HttpGet]
		public dynamic GetServerStats()
		{
			return Mongo.GetServerStats();
		}

		[HttpGet]
		public dynamic GetSettings()
		{
			return Mongo.GetSettings();
		}

		[HttpPost]
		public void SaveSettings(string settings)
		{
			Mongo.SaveSettings(JsonConvert.DeserializeObject(settings));
		}

		[HttpGet]
		public void TestHub()
		{
			UpdateHub.Test(updateContext);
		}

		[HttpGet]
		public void TestPush()
		{

		}

		// Returns a 202 Accepted to tell the mobile app that this is a kReport server
		[HttpGet]
		public void IsApp()
		{
			Response.StatusCode = 202;
		}

		// Site & app logins
		[HttpPost]
		public string Login(LoginUserInfo info)
		{
			kUser user;

			try
			{
				if (info.UsernameOrEmail.Contains('@'))
				{
					//Log in with email
					user = Mongo.GetUserByEmail(info.UsernameOrEmail);
				}
				else
				{
					//Log in with username
					user = Mongo.GetUserByName(info.UsernameOrEmail);
				}

				if (!PasswordHash.ValidatePassword(info.Password, user.Password))
					throw new Exception();
			}
			catch
			{
				//Don't tell the user exactly what went wrong with the login process
				Response.StatusCode = 403;
				return "Incorrect username/email or password.";
			}

			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Role, user.Admin ? "Admin" : "User")
			};
			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			Context.Authentication.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

			string welcomeBack = user.GetName();

			Response.StatusCode = 200;
			return "Welcome back, " + welcomeBack + "!";
		}

		[HttpGet]
		public ActionResult Logout()
		{
			Context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index", "Home");
		}

		[HttpPost]
		public string FirstUser(FirstUserInfo info)
		{
			if (info == null)
			{
				Response.StatusCode = 400;
				return "Please fill out all fields.";
			}

			if (Mongo.HasUsers())
			{
				Response.StatusCode = 400;
				return "A user already exists in the database.";
			}

			if (info.Password != info.ConfirmPassword)
			{
				Response.StatusCode = 400;
				return "Passwords did not match.";
			}

			kUser user = new kUser();
			user.Admin = true;
			user.Email = info.Email;
			user.Password = PasswordHash.CreateHash(info.Password);

			Mongo.AddUser(user);
			Login(new LoginUserInfo
			{
				UsernameOrEmail = info.Email,
				Password = info.Password
			});

			Response.StatusCode = 200;
			return "First user created!  You have been automatically logged in.";
		}

		// Plugin-facing APIs
		// POST k/Report?kRequest.Sender.Name=Kredit&kRequest.Sender.ID3=U:1:49061560&Key=testkey etc
		[HttpPost]
		public void Report(ApiReport req)
		{
			if (Helpers.ValidateKey(req.Key))
			{
				Save(req.KRequest);
				UpdateHub.NewRequest(updateContext, req.KRequest);
				Response.StatusCode = 200;
			}
			else Response.StatusCode = 412;
		}

		[HttpPost]
		public void Middleman(ApiMiddleman req)
		{
			if (Helpers.ValidateKey(req.Key))
			{
				Save(req.KRequest);
				UpdateHub.NewRequest(updateContext, req.KRequest);
				Response.StatusCode = 200;
			}
			else Response.StatusCode = 412;
		}

		private void Save(kRequest req)
		{
			Mongo.IncrementRequestsToday(req);
			Mongo.SaveRequest(req);
		}

		/// <summary>
		/// Changes a record's "done" attribute
		/// </summary>
		/// <param name="ids">IDs of records to change</param>
		/// <param name="done">True to mark as done, false to mark as not-done</param>
		[HttpPost]
		public void Done(string[] ids, bool done)
		{
			ObjectId[] oids = StringsToObjectIds(ids);
			Mongo.Done(oids, done);
		}

		[HttpPost]
		public void Delete(string[] ids)
		{
			ObjectId[] oids = StringsToObjectIds(ids);
			Mongo.Delete(oids);
		}

		private ObjectId[] StringsToObjectIds(string[] ids)
		{
			return ids.Select(i => new ObjectId(i)).ToArray();
		}
	}
}