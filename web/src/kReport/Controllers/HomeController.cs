using Microsoft.AspNet.Mvc;

namespace kReport.Controllers
{
	public class HomeController : BaseController
	{
		public IActionResult Index()
		{
			return View(model);
		}
	}
}