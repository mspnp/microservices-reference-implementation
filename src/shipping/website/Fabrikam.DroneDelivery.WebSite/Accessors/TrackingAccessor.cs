using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.ApiClient;
using Fabrikam.DroneDelivery.ApiClient.Model;
using Fabrikam.DroneDelivery.WebSite.Common;
using Fabrikam.DroneDelivery.WebSite.Interfaces;
using Newtonsoft.Json;

namespace Fabrikam.DroneDelivery.WebSite.Accessors
{
    public class TrackingAccessor : ITrackingAccessor
    {
        private TrackingClient _trackingClient;

        public TrackingAccessor(IHttpClientFactory clientFactory)
        {
            this._trackingClient = new TrackingClient(clientFactory.CreateClient("delivery"));
        }

        public async Task<DeliveryResponse> AddDeliveryRequest(DeliveryRequest deliveryRequest)
        {
            return await this._trackingClient.AddDeliveryRequest(deliveryRequest);
        }

        public async Task<Delivery> GetDelivery(Guid deliveryId)
        {
            return await this._trackingClient.GetDelivery(deliveryId);
        }

        public async Task<DroneLocation> GetDroneLocation(Guid deliveryId)
        {
            return await this._trackingClient.GetDroneLocation(deliveryId);
        }
    }
}
