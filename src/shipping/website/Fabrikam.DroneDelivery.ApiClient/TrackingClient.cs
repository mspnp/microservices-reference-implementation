using Fabrikam.DroneDelivery.ApiClient.Model;
using Fabrikam.DroneDelivery.WebSite.Common;
using System;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.ApiClient
{
    public class TrackingClient
    {
        private string _baseUrl;
        
        public TrackingClient(string url)
        {
            _baseUrl = url;
        }

        public async Task<Delivery> GetDelivery(Guid deliveryId)
        {
            return await RestClient.Get<Delivery>($"{this._baseUrl}/api/deliveries/{deliveryId}");
        }

        public async Task<Location> GetDroneLocation(Guid deliveryId)
        {
            return await RestClient.Get<Location>($"{this._baseUrl}/api/deliveries/{deliveryId}/status");
        }
    }
}
