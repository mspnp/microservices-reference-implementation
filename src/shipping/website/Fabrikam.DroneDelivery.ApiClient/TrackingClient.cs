using Fabrikam.DroneDelivery.ApiClient.Model;
using Fabrikam.DroneDelivery.WebSite.Common;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.ApiClient
{
    public class TrackingClient
    {
        private RestClient _client;

        public TrackingClient(HttpClient httpClient)
        {
            _client = new RestClient(httpClient);
        }

        public async Task<DeliveryResponse> AddDeliveryRequest(DeliveryRequest deliveryRequest)
        {
            var delivery = await this._client.Post<DeliveryResponse>($"api/deliveryrequests", deliveryRequest);
            return delivery;
        }

        public async Task<Delivery> GetDelivery(Guid deliveryId)
        {
            for (var i = 0; i <= 5; i++)
            {
                try
                {
                    return await this._client.Get<Delivery>($"api/deliveries/{deliveryId}");
                }
                catch (Exception)
                {
                    if (i >= 5)
                    {
                        throw;
                    }
                    await Task.Delay(1000);
                }
            }
            return null;
        }

        public async Task<DroneLocation> GetDroneLocation(Guid deliveryId)
        {
            var droneLocation = await this._client.Get<DroneLocation>($"api/Drone/{deliveryId}");
            return droneLocation;
        }
    }
}
