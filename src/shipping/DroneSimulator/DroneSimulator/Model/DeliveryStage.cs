using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DroneSimulator.Model
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
