using Fabrikam.DroneDelivery.Drone.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.Drone
{
    public class RouteCalculator
    {
        public List<(double lat, double lon)> GenerateRoute(Location start, Location end)
        {
            var positionHandler = new PositionHandler();

            var bearing = positionHandler.CalculateBearing(new Position(start.Latitude, start.Longitude), new Position(end.Latitude, end.Longitude));
            var distance = positionHandler.CalculateDistance(new Position(start.Latitude, start.Longitude), new Position(end.Latitude, end.Longitude), DistanceType.Kilometers);

            var locationList = new List<(double lat, double lon)>();

            (double lat, double lon) location = (start.Latitude, start.Longitude);
            for (double d = 0; d <= distance; d = d + 0.1)
            {
                var newDestiation = positionHandler.Destination(location, 0.1, bearing);
                locationList.Add(newDestiation);
                location = newDestiation;
            }

            return locationList;
        }

        public List<(double lat, double lon)> GetDroneToStartRoute(Location start)
        {
            var positionHandler = new PositionHandler();
            var droneStart = positionHandler.Destination((start.Latitude, start.Longitude), 5, 0);

            var droneRoute = GenerateRoute(new Location() { 
                Altitude= 0,
                Latitude = droneStart.Lat,
                Longitude = droneStart.Lon
            }, start);

            return droneRoute;
        }
    }
}
