using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Globalization;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using QRuhmaReport.Models;

namespace QRuhmaReport.Controllers
{
    public class StudentsController : Controller
    {
        IConfiguration config;

        public StudentsController(IConfiguration config)
        {
            this.config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery(Name ="q")]string query)
        {
            query = String.IsNullOrEmpty(query) ? "select * from s" : query;
            return Content(await GetStudents(query), "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody]List<Student> students)
        {
            foreach (Student student in students)
            {
                string resource = "dbs/students/colls/registration";
                string body = JsonConvert.SerializeObject(student);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                await this.ExecuteDocumentDbRequest(
                resource: resource,
                resourceType: "docs",
                verb: "post",
                action: httpClient => httpClient.PostAsync(resource + "/docs", content),
                isQuery: false,
                isUpsert: true);
            }

            return Ok();
        }

        async Task<string> GetStudents(string query)
        {
            string body = JsonConvert.SerializeObject(new 
            {
                query = query,
                parameters = new string[0]
            });
            string resource = "dbs/students/colls/registration";
            return await ExecuteDocumentDbRequest(
                resource: resource,
                resourceType: "docs",
                verb: "post",
                action: async (httpClient) =>
                {
                    var content = new StringContent(body);
                    content.Headers.Remove("Content-Type");
                    content.Headers.TryAddWithoutValidation("Content-Type", "application/query+json");
                    HttpResponseMessage response = await httpClient.PostAsync(resource + "/docs", content);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                },
                isQuery: true,
                isUpsert: false
             );
        }

        async Task<T> ExecuteDocumentDbRequest<T>(string resource, string resourceType, string verb, Func<HttpClient, Task<T>> action, bool isQuery, bool isUpsert)
        {
            string baseUrl = this.config.GetValue<string>("documentdb_baseurl");
            string key = this.config.GetValue<String>("documentdb_key");
            return await ExecuteDocumentDbRequestAsync(baseUrl, key, resource, resourceType, verb, action, isQuery, isUpsert);
        }

        private async Task<T> ExecuteDocumentDbRequestAsync<T>(string baseUrl, string key, string resource, string resourceType, string verb, Func<HttpClient, Task<T>> action, bool isQuery, bool isUpsert)
        {
            string keyType = "master";
            string tokenVersion = "1.0";
            string apiVersion = "2015-12-16";

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(baseUrl);
                string utcDate = DateTime.UtcNow.ToString("r").ToLowerInvariant();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("authorization", GenerateAuthToken(verb, resource, resourceType, key, keyType, tokenVersion, utcDate));
                httpClient.DefaultRequestHeaders.Add("x-ms-date", utcDate);
                httpClient.DefaultRequestHeaders.Add("x-ms-version", apiVersion);

                if (isQuery)
                {
                    httpClient.DefaultRequestHeaders.Add("x-ms-documentdb-isquery", isQuery.ToString());
                }
                else if (isUpsert)
                {
                    httpClient.DefaultRequestHeaders.Add("x-ms-documentdb-is-upsert", isUpsert.ToString());
                }

                T response = await action(httpClient);
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
