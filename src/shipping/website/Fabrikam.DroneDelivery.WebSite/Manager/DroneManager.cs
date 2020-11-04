using System;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.ApiClient.Model;
using Fabrikam.DroneDelivery.WebSite.Interfaces;

namespace Fabrikam.DroneDelivery.WebSite.Manager
{
    public class DroneManager : IDroneManager
    {
        private ITrackingAccessor _trackingAccessor;
        private IGeoCodeService _geoCodeService;

        public DroneManager(ITrackingAccessor trackingAccessor, IGeoCodeService geoCodeService)
        {
            this._trackingAccessor = trackingAccessor;
            this._geoCodeService = geoCodeService;
        }

        public async Task<DeliveryResponse> AddDeliveryRequest(DeliveryRequest deliveryRequest)
        {
            var pickup = await this._geoCodeService.ConvertAddressToLocation(deliveryRequest.pickupLocation);
            var dropoff = await this._geoCodeService.ConvertAddressToLocation(deliveryRequest.dropOffLocation);

            if (pickup == null || dropoff == null)
            {
                throw new Exception("Addresses could not be resolved");
            }

            deliveryRequest.pickupLocation = $"{pickup.Point.Coordinates[0]},{pickup.Point.Coordinates[1]}";
            deliveryRequest.dropOffLocation = $"{dropoff.Point.Coordinates[0]},{dropoff.Point.Coordinates[1]}";

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
