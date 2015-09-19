using kReport.Models;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace kReport.Controllers
{
	[Authorize]
	public class AdminController : BaseController
	{
		public IActionResult Index()
		{
			return View(model);
		}

		[AllowAnonymous]
		public IActionResult Login(string returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			return View(model);
		}

		[AllowAnonymous]
		public IActionResult Start()
		{
			if (!Mongo.HasUsers())
				return View(model);

			return RedirectToAction("Index");
		}
	}
}
