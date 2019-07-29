using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CyonClient
{
    public class CyonContext
    {
        internal string baseUrl = "https://my.cyon.ch";
        internal HttpClient client;
        internal CookieContainer cookies = new CookieContainer();
        internal HttpClientHandler handler = new HttpClientHandler();

        private Domain currentDomain;
        private readonly string username, password;        

        /// <summary>
        /// Name of the cyon Account.
        /// This is listen unter Webhosting/Overview in the section accessdata webhosting as the field username
        /// </summary>
        public string AccountName { get; private set; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="AccountName">Name of the cyon Account. This is listen unter Webhosting/Overview in the section accessdata webhosting as the field username</param>
        /// <param name="Username">Username for authentication against https://my.cyon.ch</param>
        /// <param name="Password">Passwort for authentication against https://my.cyon.ch</param>
        public CyonContext(string AccountName, string Username,
                              string Password)
        {
            this.AccountName = AccountName ?? throw new ArgumentNullException(nameof(AccountName));
            username = Username ?? throw new ArgumentNullException(nameof(Username));
            password = Password ?? throw new ArgumentNullException(nameof(Password));
        }

        private async Task InitializeClient()
        {
            Dictionary<string, string> content = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "pathname", "/" }
            };

            client = new HttpClient(handler);
            handler.CookieContainer = cookies;

            var authenticationResult = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUrl + "/auth/index/dologin-async"))
            {
                Content = new FormUrlEncodedContent(content)
            };

            var result = await client.SendAsync(authenticationResult);

            result.EnsureSuccessStatusCode();

            var validateResult = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUrl))
            {
                Content = new FormUrlEncodedContent(content)
            };

            var response = await client.SendAsync(validateResult);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Domain>> GetDomains()
        {
            if (client == null)
                await InitializeClient();

            HttpResponseMessage response = await client.GetAsync(baseUrl + "/domain/manage/list-async");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            //Convert response
            List<Domain> results = new List<Domain>();
            var result = JsonConvert.DeserializeObject<JObject>(jsonString);

            var reader = result.First.ToObject<JProperty>().First.ToObject<JArray>();

            foreach (var d in reader)
            {
                string val = d[0].Value<string>();
                if (val.Contains(" "))
                    results.Add(new Domain(val.Substring(0, val.IndexOf(" ")), this));
                else
                    results.Add(new Domain(val, this));
            }

            return results;
        }

        internal async void ChangeDomain(Domain domain)
        {
            if(currentDomain == null || currentDomain != domain)
            {
                client.DefaultRequestHeaders.Add("Referer", baseUrl + "/domain/dnseditor");
                HttpResponseMessage responseChangeDomain = await client.GetAsync(baseUrl + "/user/environment/setdomain/d/" + domain.Name + "/gik/account%3A" + AccountName);

                responseChangeDomain.EnsureSuccessStatusCode();

                currentDomain = domain;
            }            
        }
    }
}

