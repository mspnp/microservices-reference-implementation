using System;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.ApiClient.Model;
using Fabrikam.DroneDelivery.WebSite.Interfaces;

namespace Fabrikam.DroneDelivery.WebSite.Manager
{
    public class DroneManager : IDroneManager
    {
        private ITrackingAccessor _trackingAccessor;

        public DroneManager(ITrackingAccessor trackingAccessor)
        {
            this._trackingAccessor = trackingAccessor;
        }
        public async Task<DeliveryResponse> AddDeliveryRequest(DeliveryRequest deliveryRequest)
        {
            return await this._trackingAccessor.AddDeliveryRequest(deliveryRequest);
        }
        public async Task<Delivery> GetDelivery(Guid deliveryId)
        {
            return await this._trackingAccessor.GetDelivery(deliveryId);
        }

        public async Task<DroneLocation> GetDroneLocation(Guid deliveryId)
        {
            return await this._trackingAccessor.GetDroneLocation(deliveryId);
        }
    }
}
