using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using kReport.Models;
using Microsoft.AspNet.Mvc.Filters;
using System.Security.Claims;

namespace kReport.Controllers
{
	public class Viewmodel
	{
		public kUser User { get; set; }
		public string[] Themes { get; set; }
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
					model.User = user;
				}
				catch { model.User = null; }

				//Get themes
				List<string> themes = new List<string>();
				foreach (var theme in Mongo.GetEnabledThemes())
				{
					string file = Url.Content("~/Style/Themes/" + theme + ".css");
					themes.Add(file);
				}
				model.Themes = themes.ToArray();
			}

			base.OnActionExecuting(context);
		}
	}
}
