using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DroneSimulator.Model
{
    public class DeliveryTracking
    {
        public string DeliveryId { get; set; }
        public DeliveryStage Stage { get; set; }
        public Location Location { get; set; }
    }
}
