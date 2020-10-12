using Fabrikam.DroneDelivery.ApiClient.Model;
using System;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.WebSite.Interfaces
{
    public interface ITrackingAccessor
    {
        Task<Delivery> GetDelivery(Guid deliveryId);
        Task<DroneLocation> GetDroneLocation(Guid deliveryId);
        Task<DeliveryResponse> AddDeliveryRequest(DeliveryRequest deliveryRequest);
    }
}
