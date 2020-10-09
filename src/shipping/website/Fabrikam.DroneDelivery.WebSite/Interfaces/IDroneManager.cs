using Fabrikam.DroneDelivery.ApiClient.Model;
using System;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.WebSite.Interfaces
{
    public interface IDroneManager
    {
        Task<Delivery> GetDelivery(Guid deliveryId);

        Task<Location> GetDroneLocation(Guid deliveryId);
    }
}
