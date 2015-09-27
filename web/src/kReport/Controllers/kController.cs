﻿using kReport.Hubs;
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
using System.Net.Mail;
using System.Net;

namespace kReport.Controllers
{
	//Allow API requests to store a Key somewhere for verification
	//TODO: Do this with interfaces
	public class ApiReport
	{
		public Report kRequest { get; set; }
		public string Key { get; set; }
	}

	public class ApiMiddleman
	{
		public Middleman kRequest { get; set; }
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

		[HttpPost]
		public void SaveSettings(string settings)
		{
			if (Context.User.IsInRole("Admin"))
			{
				Mongo.SaveSettings(JsonConvert.DeserializeObject(settings));
			}
			else Response.StatusCode = 401;
		}

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

		[HttpGet]
		public void TestHub()
		{
			if (Context.User.IsInRole("Admin"))
			{
				UpdateHub.Test(updateContext);
			}
			else Response.StatusCode = 401;
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

		[HttpGet]
		public ActionResult Logout()
		{
			Context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index", "Home");
		}

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

		// Plugin-facing APIs
		// POST k/Report?kRequest.Sender.Name=Kredit&kRequest.Sender.ID3=U:1:49061560&Key=testkey etc
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
			if (Context.User.Identity.IsAuthenticated)
			{
				ObjectId[] oids = StringsToObjectIds(ids);
				Mongo.Done(oids, done);
			}
			else Response.StatusCode = 401;
		}

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

		private ObjectId[] StringsToObjectIds(string[] ids)
		{
			return ids.Select(i => new ObjectId(i)).ToArray();
		}
	}
}