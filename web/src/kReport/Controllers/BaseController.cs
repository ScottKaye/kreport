using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using kReport.Infrastructure;
using kReport.Models;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Framework.Internal;
using MongoDB.Bson;
using System.Security.Claims;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace kReport.Controllers
{
	public class BaseController : Controller
	{
		public Viewmodel model { get; set; }

		public BaseController()
		{
			model = new Viewmodel();
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			//Try to get current user
			try
			{
				string id = Context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
				kUser user = Mongo.GetUserById(id);
				model.User = user;
			}
			catch { model.User = null; }

			base.OnActionExecuting(context);
		}
	}
}
