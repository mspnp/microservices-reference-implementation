using Fabrikam.DroneDelivery.Drone.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.Drone
{
    public class DeliveryApi
    {
        private string apiUrl = null;

        public DeliveryApi(string apiUrl)
        {
            this.apiUrl = apiUrl;
        }

        public async Task<Delivery> GetDroneDelivery(string deliveryId)
        {
            var response = await Client.GetAsync($"{this.apiUrl}/api/Deliveries/{deliveryId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var delivery = JsonConvert.DeserializeObject<Delivery>(json);
            return delivery;
        }

        public async Task<DroneLocation> GetDroneLocation(string deliveryId)
        {
            try
            {
                var response = await Client.GetAsync($"{this.apiUrl}/api/Drone/{deliveryId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var delivery = JsonConvert.DeserializeObject<DroneLocation>(json);
                return delivery;
            } 
            catch(Exception)
            {
                return new DroneLocation()
                {
                    LastKnownLocation = new LastKnownLocation()
                    {
                        Altitude = 0,
                        Latitude = 0,
                        Longitude = 0
                    },
                    Stage = DeliveryStage.Created
                };
            }
        }

        public async Task UpdateDroneLocation(string deliveryId, DeliveryStage stage, double altitude, double latitude, double longitude)
        {
            try
            {
                Console.Write($"Stage: {stage} | Altitude: {altitude} | Latitude: {latitude} | Longitude: {longitude} | ");

                var deliveryTracking = new DeliveryTracking()
                {
                    DeliveryId = deliveryId,
                    Stage = stage,
                    Location = new Fabrikam.DroneDelivery.Drone.Model.Location()
                    {
                        Altitude = altitude,
                        Latitude = latitude,
                        Longitude = longitude
                    }
                };

                var json = JsonConvert.SerializeObject(deliveryTracking);

                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                var response = await Client.PutAsync($"{this.apiUrl}/api/Drone/{deliveryId}", content);

                Console.WriteLine($"Status: {response.StatusCode}");
            } 
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static HttpClient client = null;

        public HttpClient Client
        {
            get
            {
                if (client == null)
                {
                    var httpClientHandler = new HttpClientHandler();
                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                    client = new HttpClient(httpClientHandler);
                }
                return client;
            }
        }
    }
}
