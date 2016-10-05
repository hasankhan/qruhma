using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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
