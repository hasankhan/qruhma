using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        public async Task<IActionResult> Index([FromQuery(Name ="q")]string query, [FromQuery(Name = "n")]int? top)
        {
            query = String.IsNullOrEmpty(query) ? "select * from s" : query;
            return Content(await GetStudents(query, top), "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody]List<Student> students)
        {
            await Task.WhenAll(students.Select(student =>
            {
                string resource = "dbs/students/colls/registration";
                string body = JsonConvert.SerializeObject(student);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                content.Headers.Add("x-ms-documentdb-is-upsert", "True");
                return (Task)this.ExecuteDocumentDbRequest(
                    resource: resource,
                    resourceType: "docs",
                    verb: "post",
                    action: httpClient => httpClient.PostAsync(resource + "/docs", content));
            }));

            return Ok();
        }

        async Task<string> GetStudents(string query, int? top)
        {
            var output = new StringBuilder();
            output.Append("[");
            string body = JsonConvert.SerializeObject(new 
            {
                query = query,
                parameters = new string[0]
            });

            string continuationToken = null;
            while (true)
            {
                string resource = "dbs/students/colls/registration";
                string result = await ExecuteDocumentDbRequest(
                    resource: resource,
                    resourceType: "docs",
                    verb: "post",
                    action: async (httpClient) =>
                    {
                        var content = new StringContent(body);
                        if (top.HasValue)
                        {
                            content.Headers.Add("x-ms-max-item-count", top.Value.ToString());
                        }
                        if (continuationToken != null)
                        {
                            content.Headers.Add("x-ms-continuation", continuationToken);
                        }
                        content.Headers.Add("x-ms-documentdb-isquery", "True");
                        content.Headers.Remove("Content-Type");
                        content.Headers.TryAddWithoutValidation("Content-Type", "application/query+json");
                        HttpResponseMessage response = await httpClient.PostAsync(resource + "/docs", content);
                        response.EnsureSuccessStatusCode();
                        continuationToken = GetContinuationToken(response);
                        return await response.Content.ReadAsStringAsync();
                    }
                 );
                output.Append(result);
                if (continuationToken == null)
                {
                    break;
                }
                else
                {
                    output.Append(",");
                }
            }
            output.Append("]");
            return output.ToString();
        }

        static string GetContinuationToken(HttpResponseMessage response)
        {
            string continuationToken;
            IEnumerable<string> tokens;
            if (response.Headers.TryGetValues("x-ms-continuation", out tokens))
            {
                continuationToken = tokens.FirstOrDefault();
            }
            else
            {
                continuationToken = null;
            }
            return continuationToken;
        }

        async Task<T> ExecuteDocumentDbRequest<T>(string resource, string resourceType, string verb, Func<HttpClient, Task<T>> action)
        {
            string baseUrl = this.config.GetValue<string>("documentdb_baseurl");
            string key = this.config.GetValue<String>("documentdb_key");
            return await ExecuteDocumentDbRequestAsync(baseUrl, key, resource, resourceType, verb, action);
        }

        async Task<T> ExecuteDocumentDbRequestAsync<T>(string baseUrl, string key, string resource, string resourceType, string verb, Func<HttpClient, Task<T>> action)
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
