using System;
using System.Collections.Generic;
using System.Text;

namespace Fabrikam.DroneDelivery.ApiClient.Model
{
    public class DroneLocation
    {
        public int Stage { get; set; }

        public Location LastKnownLocation { get; set; }

        public string PickupETA { get; set; }

        public string DeliveryETA { get; set; }
    }
}
