using System;
using System.Collections.Generic;
using System.Text;

namespace DroneSimulator.Model
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
        public double Altitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
