using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using kReport.Models;

namespace kReport.Controllers
{
	public class Test
	{
		public string name { get; set; }
		public int age { get; set; }
	}

    [Route("testapi/[controller]/[action]")]
    public class ReferenceController : Controller
    {
		[HttpGet]	
		public string Get()
		{
			return "Get without parameters is disabled.";
		}

		// GET testapi/Greet/Bob/25
		[HttpGet("{name}/{age:int}")]
		public string Greet(string name, int age)
		{
			return "Hi, " + name + ", you are " + age.ToString();
		}

		// GET testapi/Middleman?name=Bob&age=25
		[HttpGet]
		public string Middleman(Test middleman)
		{
			return "Hi, " + middleman.name + ", you are " + middleman.age.ToString();
		}

		// GET testapi/Middleman?Sender.Name=Test
		// 404 Not Found (forced, will still work)
		[HttpGet]
		public string Middleman(Middleman middleman)
		{
			Response.StatusCode = 404;
			return middleman.Sender.Name;
		}
	}
}
