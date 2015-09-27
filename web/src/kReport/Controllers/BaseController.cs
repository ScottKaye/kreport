using kReport.Infrastructure;
using kReport.Models;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace kReport.Controllers
{
	public class Viewmodel
	{
		private string[] Greetings = new string[] { "Hey", "Hi", "Sup", "Hello", "Welcome back" };
		public kUser User { get; set; }
		public string[] Themes { get; set; }

		public string RandomGreeting
		{
			get
			{
				int index = new Random().Next(Greetings.Length);
				return Greetings[index];
			}
		}
	}

	public class BaseController : Controller
	{
		public Viewmodel model { get; set; }

		public BaseController()
		{
			model = new Viewmodel();
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			if (Mongo.IsConnected())
			{
				//Try to get current user
				try
				{
					string id = Context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
					kUser user = Mongo.GetUserById(id);

					//Likely a deleted user who still has claims
					if (user == null)
					{
						Context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
						model.User = null;
					}
					else
					{
						model.User = user;
					}
				}
				catch { model.User = null; }

				//Get themes
				List<string> themes = new List<string>();
				var enabledThemes = Mongo.GetEnabledThemes();
				if (enabledThemes != null)
				{
					foreach (var theme in enabledThemes)
					{
						string file = Url.Content("~/Style/Themes/" + theme + ".css");
						themes.Add(file);
					}
				}
				model.Themes = themes.ToArray();
			}

			base.OnActionExecuting(context);
		}
	}
}