using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.WebSite.Common.Utilities
{

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Owner
    {
        public string userId { get; set; }
        public string accountId { get; set; }
    }

    public class Pickup
    {
        public double altitude { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    public class Dropoff
    {
        public double altitude { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    public class CurrentLocation
    {
        public double altitude { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    public class DroneLocations
    {
        public string id { get; set; }
        public Owner owner { get; set; }
        public Pickup pickup { get; set; }
        public Dropoff dropoff { get; set; }
        public CurrentLocation currentLocation { get; set; }
        public string deadline { get; set; }
        public bool expedited { get; set; }
        public int confirmationRequired { get; set; }
        public string droneId { get; set; }

        public DroneLocations()
        {
            currentLocation = new CurrentLocation();
        }
    }
}
