using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Globalization;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace QRuhmaReport.Controllers
{
    public class StudentsController : Controller
    {
        IConfiguration config;

        public StudentsController(IConfiguration config)
        {
            this.config = config;
        }

        public async Task<IActionResult> Index()
        {
            return Content(await GetStudents(), "application/json");
        }

        async Task<string> GetStudents()
        {
            string resource = "dbs/students/colls/registration";
            return await ExecuteDocumentDbRequest(
                resource: resource, 
                resourceType: "docs", 
                verb: "get", 
                action: httpClient => httpClient.GetStringAsync(resource + "/docs")
             );
        }

        async Task<string> ExecuteDocumentDbRequest(string resource, string resourceType, string verb, Func<HttpClient, Task<string>> action)
        {
            string baseUrl = this.config.GetValue<string>("documentdb_baseurl");
            string key = this.config.GetValue<String>("documentdb_key");
            return await ExecuteDocumentDbRequest(baseUrl, key, resource, resourceType, verb, action);
        }

        private async Task<string> ExecuteDocumentDbRequest(string baseUrl, string key, string resource, string resourceType, string verb, Func<HttpClient, Task<string>> action)
        {
            string keyType = "master";
            string tokenVersion = "1.0";
            string apiVersion = "2015-12-16";

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(baseUrl);
                string utcDate = DateTime.UtcNow.ToString("r").ToLowerInvariant();
                httpClient.DefaultRequestHeaders.Add("authorization", GenerateAuthToken(verb, resource, resourceType, key, keyType, tokenVersion, utcDate));
                httpClient.DefaultRequestHeaders.Add("x-ms-date", utcDate);
                httpClient.DefaultRequestHeaders.Add("x-ms-version", apiVersion);
                string response = await action(httpClient);
                return response;
            }
        }

        string GenerateAuthToken(string verb, string resourceId, string resourceType, string key, string keyType, string tokenVersion, string utcDate)
        {
            var hmacSha256 = new System.Security.Cryptography.HMACSHA256 { Key = Convert.FromBase64String(key) };

            string verbInput = verb ?? "";
            string resourceIdInput = resourceId ?? "";
            string resourceTypeInput = resourceType ?? "";

            string payLoad = string.Format(CultureInfo.InvariantCulture, "{0}\n{1}\n{2}\n{3}\n{4}\n",
                    verb.ToLowerInvariant(),
                    resourceType.ToLowerInvariant(),
                    resourceId,
                    utcDate,
                    ""
            );

            byte[] hashPayLoad = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(payLoad));
            string signature = Convert.ToBase64String(hashPayLoad);

            return Uri.EscapeDataString(String.Format(CultureInfo.InvariantCulture, "type={0}&ver={1}&sig={2}", keyType, tokenVersion, signature));
        }

    }
}
