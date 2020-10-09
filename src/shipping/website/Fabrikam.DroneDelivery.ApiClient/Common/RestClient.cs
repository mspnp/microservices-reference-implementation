using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.WebSite.Common
{
    public static class RestClient
    {
        public static async Task<HttpResponseMessage> Get(string Url, string token = null, Dictionary<string, string> requestHeader = null)
        {                   
            using (HttpClient client = new HttpClient())
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
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                return await client.SendAsync(request);
            }
        }

        public static async Task<T> Get<T>(string Url)
        {
            try
            {
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (var httpClient = new HttpClient(clientHandler))
                {
                    using (var response = await httpClient.GetAsync(Url))
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
           
        }
    }
}
