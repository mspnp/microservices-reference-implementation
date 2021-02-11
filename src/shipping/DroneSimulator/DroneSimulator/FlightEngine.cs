using DroneSimulator.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DroneSimulator
{
    public class FlightEngine
    {
        private DeliveryApi deliveryApi = null;
        ILogger<FlightEngine> _logger = null;
        public FlightEngine(DeliveryApi deliveryApi, ILogger<FlightEngine> logger)
        {
            this.deliveryApi = deliveryApi;
            _logger = logger;
        }

        public async Task ExecuteDroneDelivery(string deliveryId)
        {
            try
            {
                var routeCalculator = new RouteCalculator();

                _logger.LogInformation($"Getting Delivery Details for {deliveryId}");

                var delivery = await this.deliveryApi.GetDroneDelivery(deliveryId);
                var droneLocation = await this.deliveryApi.GetDroneLocation(deliveryId);

                _logger.LogInformation($"Drone Flying to Pickup for {deliveryId}");
                await Task.Delay(5000);
                var droneToPickupRoute = routeCalculator.GetDroneToStartRoute(delivery.Pickup);

                await TakeOff(Model.DeliveryStage.HeadedToPickup, deliveryId, new DroneLocation()
                {
                    LastKnownLocation = new LastKnownLocation()
                    {
                        Latitude = droneToPickupRoute[0].lat,
                        Longitude = droneToPickupRoute[0].lon
                    }
                }, 300);

                foreach (var location in droneToPickupRoute)
                {
                    await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToPickup, GetAltitude(), location.lat, location.lon);
                    //await Task.Delay(5000);
                }

                _logger.LogInformation($"Picking up Package for {deliveryId}"); 

                droneLocation = await this.deliveryApi.GetDroneLocation(deliveryId);

                await Land(Model.DeliveryStage.HeadedToPickup, deliveryId, droneLocation);
                //await Task.Delay(20000);
                await TakeOff(Model.DeliveryStage.HeadedToDropoff, deliveryId, droneLocation, 300);
                
                _logger.LogInformation($"Drone Flying to Dropoff for {deliveryId}");
                // Console.WriteLine("Drone Flying to Dropoff...");

                var pickupToDropOffRoute = routeCalculator.GenerateRoute(delivery.Pickup, delivery.Dropoff);
                foreach (var location in pickupToDropOffRoute)
                {
                    await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.HeadedToDropoff, GetAltitude(), location.lat, location.lon);
                    //await Task.Delay(5000);
                }

                droneLocation = await this.deliveryApi.GetDroneLocation(deliveryId);

                await Land(Model.DeliveryStage.HeadedToDropoff, deliveryId, droneLocation);

                //await Task.Delay(30000);
                await deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.Completed, 0, delivery?.Dropoff?.Latitude ?? 0, delivery?.Dropoff?.Longitude ?? 0);
                //await Task.Delay(30000);

                droneLocation = await this.deliveryApi.GetDroneLocation(deliveryId);

                await TakeOff(Model.DeliveryStage.Completed, deliveryId, droneLocation, 300);
                //await Task.Delay(5000);
                await this.deliveryApi.UpdateDroneLocation(deliveryId, Model.DeliveryStage.Completed, 0, 0, 0);

                _logger.LogInformation("Delivery Complete for deliveryId" + deliveryId);
            }
            catch (AggregateException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                _logger.LogError(ex, ex.Message + " Exception for deliveryId " + deliveryId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                _logger.LogError(ex, ex.Message + " Exception for deliveryId " + deliveryId);
            }
            finally
            {
                // Console.WriteLine(ex.Message);
                // Console.ReadLine();
            }
        }

        private async Task TakeOff(Model.DeliveryStage stage, string deliveryId, DroneLocation droneLocation, int targetAlititude)
        {
            for (var i = droneLocation.LastKnownLocation.Altitude; i <= targetAlititude; i = i + 50)
            {
                await deliveryApi.UpdateDroneLocation(deliveryId, stage, i,
                    droneLocation.LastKnownLocation.Latitude, droneLocation.LastKnownLocation.Longitude);
                await Task.Delay(100);
            }

            await deliveryApi.UpdateDroneLocation(deliveryId, stage, targetAlititude,
             droneLocation.LastKnownLocation.Latitude, droneLocation.LastKnownLocation.Longitude);
            await Task.Delay(200);
        }

        private async Task Land(Model.DeliveryStage stage, string deliveryId, DroneLocation droneLocation)
        {
            for (var i = droneLocation.LastKnownLocation.Altitude; i > 0; i = i - 50)
            {
                await deliveryApi.UpdateDroneLocation(deliveryId, stage, i,
                    droneLocation.LastKnownLocation.Latitude, droneLocation.LastKnownLocation.Longitude);
                //await Task.Delay(500);
            }

            await deliveryApi.UpdateDroneLocation(deliveryId, stage, 0,
                droneLocation.LastKnownLocation.Latitude, droneLocation.LastKnownLocation.Longitude);
        }

        private int GetAltitude()
        {
            var r = new Random();
            return r.Next(295, 305);
        }
    }
}
