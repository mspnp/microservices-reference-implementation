using Fabrikam.DroneDelivery.DeliveryService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.DeliveryService.Interfaces
{
    public interface ITrackingClient
    {
        Task RecieveLocation(DeliveryStatus status);

        Task SendLocation(DeliveryTracking status);
    }
}
