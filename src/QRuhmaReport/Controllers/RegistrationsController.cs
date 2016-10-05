using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace QRuhmaReport.Controllers
{
    public class RegistrationsController : Controller
    {
        IConfiguration config;
        IMemoryCache cache;

        public RegistrationsController(IConfiguration config, IMemoryCache cache)
        {
            this.config = config;
            this.cache = cache;
        }

        [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 60)]
        public async Task<IActionResult> Index(int id)
        {
            string email = this.config.GetValue<string>("almaghrib_email");
            string password = this.config.GetValue<string>("almaghrib_password");
            string pageUrl = $"https://my.almaghrib.org/admin/reports/student-roster/id/{id}";

            FileStreamResult result;
            if (!this.cache.TryGetValue<FileStreamResult>("studentsList", out result))
            {
                result = await DownloadPage(email, password, pageUrl);
                this.cache.Set("studentsList" + id, result, TimeSpan.FromMinutes(1));
            }
            return await CloneFileStreamResultAsync(result);
        }

        private async Task<FileStreamResult> CloneFileStreamResultAsync(FileStreamResult source)
        {
            var outputStream = new MemoryStream();
            source.FileStream.Seek(0, SeekOrigin.Begin);
            await source.FileStream.CopyToAsync(outputStream);
            outputStream.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(outputStream, source.ContentType);
        }

        private async Task<FileStreamResult> DownloadPage(string email, string password, string pageUrl)
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

                // download the content
                response = await httpClient.GetAsync(pageUrl);
                response.EnsureSuccessStatusCode();
                Stream contentStream = await response.Content.ReadAsStreamAsync();

                // return the content
                return new FileStreamResult(contentStream, response.Content.Headers.ContentType.MediaType);
            }
        }

    }
}
