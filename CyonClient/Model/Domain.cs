using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CyonClient
{
    public class Domain
    {
        public string Name { get; set; }
        public ReadOnlyCollection<DNSRecord> DNSRecords
        {
            get
            {
                if (dnsRecords == null)
                    GetDNSRecord();
                return new ReadOnlyCollection<DNSRecord>(dnsRecords);
            }
        }

        private readonly CyonContext context;
        private List<DNSRecord> dnsRecords;

        internal Domain(string Name, CyonContext context)
        {
            this.context = context;
            this.Name = Name;
        }

        public Task CreateARecord(string Name, string Value, DNSTTL TTL)
        {
            return CreateDNSRecord(DNSType.A, Name, Value, TTL);
        }

        public Task CreateAAAARecord(string Name, string Value, DNSTTL TTL)
        {
            return CreateDNSRecord(DNSType.AAAA, Name, Value, TTL);
        }

        public Task CreateCNAMERecord(string Name, string Value, DNSTTL TTL)
        {
            return CreateDNSRecord(DNSType.CNAME, Name, Value, TTL);
        }

        public Task CreateTXTRecord(string Name, string Value, DNSTTL TTL)
        {
            return CreateDNSRecord(DNSType.TXT, Name, Value, TTL);
        }

        public async Task UpdateDNSRecord(DNSRecord Record)
        {
            if (String.IsNullOrEmpty(Record.RecordHash))
                throw new ArgumentNullException("DNSRecord is not fully initialized. An Record Hash must be present.");

            Dictionary<string, string> content = GetDNSRequestParameters(Record.Type, Record.Name, Record.Value, Record.TTL);
            content.Add("hash", Record.RecordHash);
        
            var createResult = new HttpRequestMessage(HttpMethod.Post, new Uri(context.baseUrl + "/domain/dnseditor/edit-record-async"))
            {
                Content = new FormUrlEncodedContent(content)
            };

            context.ChangeDomain(this);
            var response = await context.client.SendAsync(createResult);

            EvaluateDNSModifyResponse(response, DNSModificationType.Update);
        }

        public async Task DeleteDNSRecord(DNSRecord record)
        {
            context.ChangeDomain(this);

            Dictionary<string, string> content = new Dictionary<string, string>
            {
                { "hash", WebUtility.UrlEncode(record.RecordHash) },
                {
                    "identitfier",
                    record.Type.ToString() +
                        WebUtility.UrlEncode("|") +
                        record.Name +
                        WebUtility.UrlEncode("|") +
                        record.Value
                }
            };

            var deleteRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(context.baseUrl + "/domain/dnseditor/delete-record-async"))
            {
                Content = new FormUrlEncodedContent(content)
            };

            context.ChangeDomain(this);
            var response = await context.client.SendAsync(deleteRequest);

            response.EnsureSuccessStatusCode();

            EvaluateDNSModifyResponse(response, DNSModificationType.Delete);
        }

        private async void GetDNSRecord()
        {
            context.ChangeDomain(this);

            HttpResponseMessage response = context.client.GetAsync(context.baseUrl + "/domain/dnseditor/list-async").Result;
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            //Convert response
            dnsRecords = new List<DNSRecord>();
            var result = JsonConvert.DeserializeObject<JObject>(jsonString);


            var reader = result.First.ToObject<JProperty>().First.ToObject<JArray>();

            foreach (var d in reader)
            {
                DNSRecord record = new DNSRecord()
                {
                    Name = d[0].Value<string>(),
                    Value = d[3].Value<string>()
                };

                // Evaluate TTL
                var ttlResponse = d[1].Value<string>();
                switch (ttlResponse)
                {
                    case "15 Minuten":
                        record.TTL = DNSTTL.QuarterHour;
                        break;
                    case "1 Stunde":
                        record.TTL = DNSTTL.OneHour;
                        break;
                    case "4 Stunden":
                        record.TTL = DNSTTL.FourHours;
                        break;
                    case "24 Stunden":
                        record.TTL = DNSTTL.OneDay;
                        break;
                }

                // Evaluate DNS Record Type
                var recordTypeResponse = d[2].Value<string>();
                var typeValue = recordTypeResponse.Substring(
                                        recordTypeResponse.IndexOf(">") + 1,
                                        recordTypeResponse.IndexOf("</") - recordTypeResponse.IndexOf(">") - 1);

                if (typeValue.Contains(" "))
                    typeValue = typeValue.Substring(0, typeValue.IndexOf(" "));

                record.Type = Enum.Parse<DNSType>(typeValue);

                dnsRecords.Add(record);

                // Evaluate RecordHash
                var customData = d[4].Value<string>();

                var recordHashStart = customData.Substring(customData.IndexOf("hash/") + 5);
                record.RecordHash = recordHashStart.Substring(0, recordHashStart.IndexOf("\""));
            }
        }

        private async Task CreateDNSRecord(DNSType Type, string Name, string Value, DNSTTL TTL)
        {
            if (dnsRecords == null)
                GetDNSRecord();
                        
            if (dnsRecords.Exists(r => r.Name == Name))
                throw new ArgumentException("A entry with the same name and type already exists.");

            Dictionary<string, string> content = GetDNSRequestParameters(Type, Name, Value, TTL);

            var createResult = new HttpRequestMessage(HttpMethod.Post, new Uri(context.baseUrl + "/domain/dnseditor/add-record-async"))
            {
                Content = new FormUrlEncodedContent(content)
            };

            context.ChangeDomain(this);
            var response = await context.client.SendAsync(createResult);

            EvaluateDNSModifyResponse(response, DNSModificationType.Create);
        }        

        /// <summary>
        /// Returns the parameter including validation to submit as FormUrlEncodedContent to my.cyon.ch.
        /// Used in create und update method.
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        /// <param name="TTL"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetDNSRequestParameters(DNSType Type, string Name, string Value, DNSTTL TTL)
        {
            Dictionary<string, string> content = new Dictionary<string, string>();

            if (Type == DNSType.CNAME)
            {
                string name = Name.EndsWith(this.Name) ? Name.Substring(0, Name.IndexOf(this.Name)) : Name;
                content.Add("zonePrefix", name);
                content.Add("zone", this.Name);
            }
            else
            {
                if (Name.EndsWith("."))
                    content.Add("zone", Name);
                else
                    content.Add("zone", Name + ".");
            }

            content.Add("ttl", ((int)TTL).ToString());
            content.Add("type", Type.ToString().ToUpper());

            if (Type == DNSType.AAAA)
                content.Add("value", WebUtility.UrlEncode(Value));
            else
                content.Add("value", Value);

            return content;
        }

        private async void EvaluateDNSModifyResponse(HttpResponseMessage response, DNSModificationType create)
        {
            response.EnsureSuccessStatusCode();

            string errorMessage = String.Empty;
            switch (create)
            {
                case DNSModificationType.Create:
                    errorMessage = "Could not create new DNS Record.";
                    break;
                case DNSModificationType.Update:
                    errorMessage = "Could not update DNS Record.";
                    break;
                case DNSModificationType.Delete:
                    errorMessage = "Could not delete DNS Record.";
                    break;
                default:
                    break;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<DNSModifyRecordResponse>(jsonString);

            if (result.Success)
                GetDNSRecord();
            else
            {
                if (result.Message != null)
                    throw new Exception($"{errorMessage} Exception is {result.Message}");
                else if (result.Form.InvalidParts != null)
                    throw new Exception($"{errorMessage} Exception is {result.Form.InvalidParts}");
                else
                    throw new Exception($"{errorMessage}");
            }
        }

    }

    internal class DNSModifyRecordResponse
    {
        [JsonProperty("status")]
        public bool Success { get; set; }
        [JsonProperty("form")]
        public DNSModifyRecordResponseForm Form { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    internal class DNSModifyRecordResponseForm
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
        [JsonProperty("invalidParts")]
        public string InvalidParts { get; set; }        
    }

    internal enum DNSModificationType
    {
        Create, Update, Delete
    }
}