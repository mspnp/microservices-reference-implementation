using System;
using System.Collections.Generic;
using System.Text;

namespace Fabrikam.DroneDelivery.Drone.Model
{
    public class DroneLocation
    {
        public DeliveryStage Stage { get; set; }
        public LastKnownLocation LastKnownLocation { get; set; }
        public string PickupETA { get; set; }
        public string DeliveryETA { get; set; }
    }

    public class LastKnownLocation
    {
        public int Altitude { get; set; }
        public int Latitude { get; set; }
        public int Longitude { get; set; }
    }
}
