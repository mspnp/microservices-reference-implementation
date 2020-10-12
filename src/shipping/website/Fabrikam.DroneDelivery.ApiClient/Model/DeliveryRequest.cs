using System;
using System.Collections.Generic;
using System.Text;

namespace Fabrikam.DroneDelivery.ApiClient.Model
{
    public class DeliveryRequest
    {
        public string confirmationRequired { get; set; }
        public string deadline { get; set; }
        public string dropOffLocation { get; set; }
        public bool expedited { get; set; }
        public string ownerId { get; set; }
        public PackageInfo packageInfo { get; set; }
        public string pickupLocation { get; set; }
        public DateTime pickupTime { get; set; }
    }
}
