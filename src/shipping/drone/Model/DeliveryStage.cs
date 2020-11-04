using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.Drone.Model
{
    public enum DeliveryStage
    {
        Created,
        Rescheduled,
        HeadedToPickup,
        HeadedToDropoff,
        Completed,
        Cancelled
    }
}
