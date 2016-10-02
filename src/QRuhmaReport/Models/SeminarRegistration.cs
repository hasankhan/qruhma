using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QRuhmaReport.Models
{
    public class SeminarRegistration
    {
        [JsonProperty(PropertyName = "seminarId")]
        public int SeminarId { get; set; }

        [JsonProperty(PropertyName = "regDate")]
        public string RegistrationDate { get; set; }

        [JsonProperty(PropertyName = "paid")]
        public bool Paid { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public int Amount { get; set; }

        [JsonProperty(PropertyName = "method")]
        public string Method { get; set; }

        [JsonProperty(PropertyName = "ref")]
        public string Reference { get; set; }

        [JsonProperty(PropertyName = "payDate")]
        public string PaymentDate { get; set; }
    }
}
