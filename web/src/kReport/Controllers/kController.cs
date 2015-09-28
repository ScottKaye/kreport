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
	#region Wrapper Classes
	/// <summary>
	/// Wrapper class to receive a report, given a valid key
	/// </summary>
	public class ApiReport
	{
		public Report kRequest { get; set; }
		public string Key { get; set; }
	}

	/// <summary>
	/// Wrapper class to receive a request for a middleman, given a valid key
	/// </summary>
	public class ApiMiddleman
	{
		public Middleman kRequest { get; set; }
		public string Key { get; set; }
	}

	/// <summary>
	/// Wrapper class to handle login information
	/// Users can log in with their username or email
	/// </summary>
	public class LoginUserInfo
	{
		public string UsernameOrEmail { get; set; }
		public string Password { get; set; }
	}

	/// <summary>
	/// The first user is required to submit only an email
	/// Both passwords must match
	/// </summary>
	public class FirstUserInfo
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public string ConfirmPassword { get; set; }
	}
	#endregion

	/// <summary>
	/// Handles incoming requests on /k/MethodName?param=value
	/// </summary>
	[Route("[controller]/[action]")]
	public class kController : Controller
	{
		private IConnectionManager _connectionManager;
		private IHubContext updateContext;

		/// <summary>
		/// Allows the controller to connect to the SignalR hub
		/// </summary>
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

		#region Get Actions
		/// <summary>
		/// Returns a list of all requests
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public IEnumerable<kRequest> GetAllRequests()
		{
			return Mongo.GetAllRequests().AsEnumerable();
		}

		/// <summary>
		/// Returns a full request, given its ID
		/// </summary>
		/// <param name="id">ID of request to return</param>
		/// <returns></returns>
		public kRequest GetRequestById(string id)
		{
			ObjectId objid;
			if (ObjectId.TryParse(id, out objid))
			{
				return Mongo.GetRequestById(objid);
			}
			return null;
		}

		/// <summary>
		/// Returns the number of requests received this week
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public int[] GetNumRequestsThisWeek()
		{
			return Mongo.GetNumRequestsThisWeek();
		}

		/// <summary>
		/// Returns the number of requests received this year
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public int[] GetNumRequestsThisYear()
		{
			return Mongo.GetNumRequestsThisYear();
		}

		/// <summary>
		/// Returns the number of requests each server has received
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic GetServerStats()
		{
			return Mongo.GetServerStats();
		}

		/// <summary>
		/// Returns a list of all users
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public List<kUser> GetAllUsers()
		{
			if (Context.User.IsInRole("Admin"))
			{
				return Mongo.GetAllUsers()
					.Select(u =>
					{
						u.Password = null;
						return u;
					}).ToList();
			}
			else Response.StatusCode = 401;
			return null;
		}

		/// <summary>
		/// Returns a user with the password set to null
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public kUser GetCurrentUser()
		{
			//The user may not be found, or may have been deleted
			try
			{
				string id = Context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
				kUser user = Mongo.GetUserById(id);
				user.Password = null;
				return user;
			}
			catch
			{
				Response.StatusCode = 404;
				return null;
			}
		}

		/// <summary>
		/// Returns a list of available settings and their parameters.
		/// Only responds to administrative requests.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public dynamic GetSettings()
		{
			if (Context.User.IsInRole("Admin"))
			{
				return Mongo.GetSettings();
			}
			else Response.StatusCode = 401;
			return null;
		}
		#endregion

		#region Login & Logout
		/// <summary>
		/// Verifies password and sets a login cookie accordingly
		/// </summary>
		/// <param name="info">Username or email of user, and their password attempt</param>
		/// <returns>A friendly message welcoming the user back.</returns>
		[HttpPost]
		public string Login(LoginUserInfo info)
		{
			kUser user;
			bool passwordSet = false;

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

				//If the password is null, the password the user entered is their new password
				if (info.Password != null && user.Password == null)
				{
					user.Password = PasswordHash.CreateHash(info.Password);
					Mongo.UpdatePassword(user);
					passwordSet = true;
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

			Response.StatusCode = 200;
			if (passwordSet)
			{
				return "Your password has been set, " + user.GetName() + ".";
			}
			else
			{
				return "Welcome back, " + user.GetName() + "!";
			}
		}

		/// <summary>
		/// Removes all login cookies and clears claims
		/// </summary>
		/// <returns>Redirect to home</returns>
		[HttpGet]
		public ActionResult Logout()
		{
			Context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index", "Home");
		}
		#endregion

		#region Administrative Actions
		/// <summary>
		/// Saves data (currently only themes/stylesheet adjustments) to the settings collection.
		/// Only responds to administrative requests.
		/// </summary>
		/// <param name="settings">Settings to save</param>
		[HttpPost]
		public void SaveSettings(string settings)
		{
			if (Context.User.IsInRole("Admin"))
			{
				Mongo.SaveSettings(JsonConvert.DeserializeObject(settings));
			}
			else Response.StatusCode = 401;
		}

		/// <summary>
		/// Saves a users details to the database
		/// Admins can update all users, but normal users can only updates their own information
		/// </summary>
		/// <param name="id">User ID to update.  This will replace the ID in the user field.</param>
		/// <param name="user">Collection of details to update for the user in the id parameter.</param>
		/// <param name="delete">If true, this will delete the user from the database.</param>
		/// <returns></returns>
		[HttpPost]
		public string SaveUser(string id, kUser user, bool delete)
		{
			//Admins can update all users, users can only update themselves
			if (!(Context.User.IsInRole("Admin") || Context.User.FindFirst(ClaimTypes.NameIdentifier).Value == id))
			{
				Response.StatusCode = 401;
				return null;
			}

			//User must have an email
			if (user.Email == null)
			{
				Response.StatusCode = 400;
				return null;
			}

			//Create or update user?
			if (id == null)
			{
				//Create new user
				user.Id = ObjectId.GenerateNewId();
				user.TimeZoneOffset = 0;
				user.NotificationSettings = new NotificationSettings
				{
					ReceiveEmail = false,
					EmailStart = new TimeSpan(0, 0, 0),
					EmailEnd = new TimeSpan(23, 59, 59)
				};
			}
			else
			{
				//Use existing user
				user.Id = ObjectId.Parse(id);

				//Admins cannot demote or delete themselves; ensure they aren't trying
				if (Context.User.FindFirst(ClaimTypes.NameIdentifier).Value == user.Id.ToString())
				{
					user.Admin = true;
					delete = false;
				}
			}

			//Only admins can delete users
			if (Context.User.IsInRole("Admin") && delete)
			{
				Mongo.DeleteUser(user);
				return null;
			}
			else
			{
				Mongo.SaveUser(user);
				return user.Id.ToString();
			}
		}

		/// <summary>
		/// Handles the creation of the first user in the database
		/// This can only be called once, and will return an error if a user already exists
		/// </summary>
		/// <param name="info">Email, password, and password confirmation wrapper class</param>
		/// <returns>Welcoming message</returns>
		[HttpPost]
		public string FirstUser(FirstUserInfo info)
		{
			if (Mongo.HasUsers())
			{
				Response.StatusCode = 400;
				return "A user already exists in the database.";
			}

			if (info == null)
			{
				Response.StatusCode = 400;
				return "Please fill out all fields.";
			}

			if (info.Password != info.ConfirmPassword)
			{
				Response.StatusCode = 400;
				return "Passwords did not match.";
			}

			kUser user = new kUser
			{
				Admin = true,
				Email = info.Email,
				Password = PasswordHash.CreateHash(info.Password),
				TimeZoneOffset = 0,
				NotificationSettings = new NotificationSettings
				{
					ReceiveEmail = true,
					EmailStart = new TimeSpan(0, 0, 0),
					EmailEnd = new TimeSpan(23, 59, 59)
				}
			};

			Mongo.AddUser(user);
			Login(new LoginUserInfo
			{
				UsernameOrEmail = info.Email,
				Password = info.Password
			});

			Response.StatusCode = 200;
			return "First user created!  You have been automatically logged in.";
		}

		/// <summary>
		/// Changes a record's "done" attribute
		/// </summary>
		/// <param name="ids">IDs of records to change</param>
		/// <param name="done">True to mark as done, false to mark as not-done</param>
		[HttpPost]
		public void Done(string[] ids, bool done)
		{
			if (Context.User.Identity.IsAuthenticated)
			{
				ObjectId[] oids = StringsToObjectIds(ids);
				Mongo.Done(oids, done);
			}
			else Response.StatusCode = 401;
		}

		/// <summary>
		/// Permanently eletes a record from the database
		/// </summary>
		/// <param name="ids">IDs of records to delete</param>
		[HttpPost]
		public void Delete(string[] ids)
		{
			if (Context.User.IsInRole("Admin"))
			{
				ObjectId[] oids = StringsToObjectIds(ids);
				Mongo.Delete(oids);
			}
			else Response.StatusCode = 401;
		}
		#endregion

		#region Sourcemod-Facing Actions
		/// <summary>
		/// Receives a strongly-typed report off the /k/Report path
		/// </summary>
		/// <example>POST k/Report?kRequest.Sender.Name=Kredit&kRequest.Sender.ID3=U:1:49061560&Key=testkey etc</example>
		/// <param name="req"></param>
		[HttpPost]
		public void Report(ApiReport req)
		{
			if (Helpers.ValidateKey(req.Key))
			{
				Save(req.kRequest);
				UpdateHub.NewRequest(updateContext, req.kRequest);
				Response.StatusCode = 200;
			}
			else Response.StatusCode = 412;
		}

		/// <summary>
		/// Receives a strongly-typed middleman request off the /k/Middleman path
		/// </summary>
		/// <param name="req"></param>
		[HttpPost]
		public void Middleman(ApiMiddleman req)
		{
			if (Helpers.ValidateKey(req.Key))
			{
				Save(req.kRequest);
				UpdateHub.NewRequest(updateContext, req.kRequest);
				Response.StatusCode = 200;
			}
			else Response.StatusCode = 412;
		}
		#endregion

		#region Misc API Actions
		/// <summary>
		/// Triggers a debug message to appear in the developer console for all users connected to SignalR
		/// Used to test if the SignalR server is working
		/// </summary>
		[HttpGet]
		public void TestHub()
		{
			if (Context.User.IsInRole("Admin"))
			{
				UpdateHub.Test(updateContext);
			}
			else Response.StatusCode = 401;
		}

		/// <summary>
		/// Sends an email to the email specified in config.json
		/// Used to test if the mail server is working
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public string TestMail()
		{
			if (Context.User.IsInRole("Admin"))
			{
				string email = Startup.Configuration.GetSection("kreport:email:user").Value;
				Mail.Send(email, "kReport Test Email", "If you are receiving this email, the kReport mail server is operational.");
				return "Email sent to " + email;
			}
			else Response.StatusCode = 401;
			return null;
		}

		/// <summary>
		/// Always returns a 202 Accepted to tell the mobile app that this is a kReport server
		/// Not the best way to do this, but if any non-kReport server gives a 202 for /k/IsApp, I don't know what to think
		/// </summary>
		[HttpGet]
		public void IsApp()
		{
			Response.StatusCode = 202;
		}
		#endregion

		#region Private API Methods
		/// <summary>
		/// Converts an array of stringly-typed ObjectIds to actual ObjectIds
		/// </summary>
		/// <param name="ids">ObjectIds as strings</param>
		/// <returns>Array of converted ObjectIds</returns>
		private ObjectId[] StringsToObjectIds(string[] ids)
		{
			return ids.Select(i => new ObjectId(i)).ToArray();
		}

		/// <summary>
		/// Used to save incoming requests to the database
		/// Also increments a few counters for the tracker/analytics
		/// </summary>
		/// <param name="req">Request to save</param>
		private void Save(kRequest req)
		{
			Mongo.IncrementRequestsToday(req);
			Mongo.SaveRequest(req);
		}
		#endregion
	}
}