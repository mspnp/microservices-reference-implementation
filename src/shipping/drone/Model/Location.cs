using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.Drone.Model
{
    public class Location
    {
        public Location() { }

        public Location(double altitude, double latitude, double longitude)
        {
            Altitude = altitude;
            Latitude = latitude;
            Longitude = longitude;
        }
        public double Altitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
