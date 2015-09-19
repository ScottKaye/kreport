using kReport.Infrastructure;
using kReport.Models;
using Microsoft.AspNet.Mvc;

namespace kReport.Controllers
{
	public class Viewmodel
	{
		public kUser User { get; set; }
	}

	public class HomeController : BaseController
	{
		public IActionResult Index()
		{
			return View(model);
		}
	}
}
