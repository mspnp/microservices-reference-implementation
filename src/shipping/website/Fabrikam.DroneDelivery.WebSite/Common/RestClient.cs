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
        public static async Task<HttpResponseMessage> GetHttpResponse(string Url, string token = null, Dictionary<string, string> requestHeader = null)
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

        public static async Task<string> GetHttpResponse(string Url)
        {
            try
            {
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (var httpClient = new HttpClient(clientHandler))
                {
                    using (var response = await httpClient.GetAsync(Url))
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        return apiResponse;
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
