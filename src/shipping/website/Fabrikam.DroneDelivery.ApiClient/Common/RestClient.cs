using Fabrikam.DroneDelivery.ApiClient.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.WebSite.Common
{
    public class RestClient
    {
        private HttpClient _httpClient;

        public RestClient(HttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> Get(string Url, string token = null, Dictionary<string, string> requestHeader = null)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(Url),
                Method = HttpMethod.Get
            };
            if (requestHeader != null)
            {
                foreach (var reqHeader in requestHeader)
                {
                    request.Headers.Add(reqHeader.Key, reqHeader.Value);
                }
            }
            if (token != null)
                this._httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return await this._httpClient.SendAsync(request);
        }

        public async Task<T> Get<T>(string Url)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                
            using (var response = await this._httpClient.GetAsync(Url))
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        public async Task<T> Post<T>(string Url, DeliveryRequest deliveryRequest)
        {
            HttpClientHandler clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(deliveryRequest);
            try
            {
                var response = await this._httpClient.PostAsync(Url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
            } 
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
