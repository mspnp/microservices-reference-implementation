using Fabrikam.DroneDelivery.Drone.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.Drone
{
    public class FlightEngine
    {
        private DeliveryApi deliveryApi = null;

        public FlightEngine(DeliveryApi deliveryApi)
        {
            this.deliveryApi = deliveryApi;
        }

        public async Task ExecuteDroneDelivery(string deliveryId)
        {
            try
            {
                var routeCalculator = new RouteCalculator();

                Console.WriteLine("Getting Delivery Details...");

                var delivery = await this.deliveryApi.GetDroneDelivery(deliveryId);
                var droneLocation = await this.deliveryApi.GetDroneLocation(deliveryId);

                await TakeOff(deliveryId, droneLocation, 300);

                Console.WriteLine("Drone Flying to Pickup...");

                var droneToPickupRoute = routeCalculator.GetDroneToStartRoute(delivery.Pickup);
                foreach (var location in droneToPickupRoute)
                {
                    await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToPickup, GetAltitude(), location.lat, location.lon);
                    await Task.Delay(5000);
                }

                Console.WriteLine("Picking up Package...");

                await Land(deliveryId, droneLocation);
                await Task.Delay(20000);
                await TakeOff(deliveryId, droneLocation, 300);

                Console.WriteLine("Drone Flying to Dropoff...");

                var pickupToDropOffRoute = routeCalculator.GenerateRoute(delivery.Pickup, delivery.Dropoff);
                foreach (var location in pickupToDropOffRoute)
                {
                    await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToDropoff, GetAltitude(), location.lat, location.lon);
                    await Task.Delay(5000);
                }

                await Land(deliveryId, droneLocation);
                await Task.Delay(20000);

                await this.deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.Completed, 0, 0, 0);
                Console.WriteLine("Delivery Complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            } 
            finally
            {
                Console.WriteLine("Press Enter to Close");
                Console.ReadLine();
            }
        }
        
        private async Task TakeOff(string deliveryId, DroneLocation droneLocation, int targetAlititude)
        {
            for (var i = droneLocation.LastKnownLocation.Altitude; i <= targetAlititude; i = i + 50)
            {
                await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToPickup, i,
                    droneLocation.LastKnownLocation.Latitude, droneLocation.LastKnownLocation.Longitude);
                await Task.Delay(500);
            }

            await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToPickup, targetAlititude,
             droneLocation.LastKnownLocation.Latitude, droneLocation.LastKnownLocation.Longitude);
        }

        private async Task Land(string deliveryId, DroneLocation droneLocation)
        {
            for (var i = droneLocation.LastKnownLocation.Altitude; i > 0; i = i - 50)
            {
                await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToPickup, i,
                    droneLocation.LastKnownLocation.Latitude, droneLocation.LastKnownLocation.Longitude);
                await Task.Delay(500);
            }

            await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToPickup, 0,
                droneLocation.LastKnownLocation.Latitude, droneLocation.LastKnownLocation.Longitude);
        }

        private int GetAltitude()
        {
            var r = new Random();
            return r.Next(295, 305);
        }
    }
}
