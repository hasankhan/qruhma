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

namespace QRuhmaReport.Controllers
{
    public class HomeController : Controller
    {
        IConfiguration config;

        public HomeController(IConfiguration config)
        {
            this.config = config;
        }

        public IActionResult Index(int id)
        {
            this.ViewBag.SeminarId = id;
            this.ViewBag.SlackApiToken = this.config.GetValue<string>("slack_api_token");
            
            return View();
        }

        public async Task<IActionResult> DownloadRoaster(int id)
        {
            string email = this.config.GetValue<string>("almaghrib_email");
            string password = this.config.GetValue<string>("almaghrib_password");
            string pageUrl = $"https://my.almaghrib.org/admin/reports/student-roster/id/{id}";
            return await DownloadPage(email, password, pageUrl);
        }

        private async Task<IActionResult> DownloadPage(string email, string password, string pageUrl)
        {
            var cookieContainer = new CookieContainer();
            using (var clientHandler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var httpClient = new HttpClient(clientHandler))
            {
                HttpResponseMessage response = await httpClient.PostAsync(
                    requestUri: "https://my.almaghrib.org/auth/authenticate",
                    content: new FormUrlEncodedContent(
                    new[] {
                        new KeyValuePair<string, string>("email", email),
                        new KeyValuePair<string, string>("passHolder", "password"),
                        new KeyValuePair<string, string>("password", password)
                    }));
                
                // make sure auth was successful
                response.EnsureSuccessStatusCode();

                // download the student roster
                response = await httpClient.GetAsync(pageUrl);
                response.EnsureSuccessStatusCode();
                Stream rosterXlsStream = await response.Content.ReadAsStreamAsync();

                // return the xls to the user
                return new FileStreamResult(rosterXlsStream, response.Content.Headers.ContentType.MediaType);
            }
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
