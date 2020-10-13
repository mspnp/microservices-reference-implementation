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
            return await this._client.Get<Delivery>($"api/deliveries/{deliveryId}");
        }

        public async Task<DroneLocation> GetDroneLocation(Guid deliveryId)
        {
            var droneLocation = await this._client.Get<DroneLocation>($"api/deliveries/{deliveryId}/status");
            return droneLocation;
        }
    }
}
