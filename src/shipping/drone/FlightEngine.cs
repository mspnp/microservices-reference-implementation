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

                Console.WriteLine("Drone Flying to Pickup...");

                var droneToPickupRoute = routeCalculator.GetDroneToStartRoute(delivery.Pickup);
                foreach (var location in droneToPickupRoute)
                {
                    await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToPickup, 300, location.lat, location.lon);
                    await Task.Delay(5000);
                }

                Console.WriteLine("Picking up Package...");

                await this.deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToPickup, 0, delivery.Pickup.Latitude, delivery.Pickup.Longitude);
                await Task.Delay(20000);

                Console.WriteLine("Drone Flying to Dropoff...");

                var pickupToDropOffRoute = routeCalculator.GenerateRoute(delivery.Pickup, delivery.Dropoff);
                foreach (var location in pickupToDropOffRoute)
                {
                    await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToDropoff, 300, location.lat, location.lon);
                    await Task.Delay(5000);
                }

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
    }
}
