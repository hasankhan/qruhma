using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Net;
using Microsoft.Net.Http.Headers;
using System.IO;
using Microsoft.Extensions.Caching.Memory;

namespace QRuhmaReport.Controllers
{
    public class HomeController : Controller
    {
        IConfiguration config;

        public HomeController(IConfiguration config)
        {
            this.config = config;
        }

        public IActionResult Index(int? id)
        {
            this.ViewBag.SlackApiToken = this.config.GetValue<string>("slack_api_token");
            this.ViewBag.SeminarId = id.GetValueOrDefault();
            this.ViewBag.UserId = this.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
