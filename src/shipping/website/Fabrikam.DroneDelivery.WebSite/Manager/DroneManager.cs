using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.WebSite.Common;
using Fabrikam.DroneDelivery.WebSite.Common.Utilities;
using Newtonsoft.Json;

namespace Fabrikam.DroneDelivery.WebSite.Manager
{
    public class DroneManager
    {
        private const string baseUrl = "https://russdronebasic2-ingest-dev.eastus.cloudapp.azure.com/api/deliveries/";
        private const string droneStatusUrl = "/status";
        private const string droneLocation = "";
        public DroneManager()
        {
        }
        public async Task<string> GetAllLocations(string deliveryId)
        {
            var allocatedLocations = await RestClient.GetHttpResponse(baseUrl + deliveryId);
            var currentLocation = await RestClient.GetHttpResponse(baseUrl + deliveryId + droneStatusUrl);
            if (!string.IsNullOrEmpty(allocatedLocations) && !string.IsNullOrEmpty(currentLocation))
            {
                DroneLocations droneLocations = JsonConvert.DeserializeObject<DroneLocations>(allocatedLocations);
                var currentLoc = JsonConvert.DeserializeObject<CurrentLocation>(currentLocation);
                droneLocations.currentLocation.altitude = currentLoc.altitude;
                droneLocations.currentLocation.longitude = currentLoc.longitude;
                droneLocations.currentLocation.latitude = currentLoc.latitude;
                return JsonConvert.SerializeObject(droneLocations);
            }
            else
            {
                return allocatedLocations;
            }

        }
        public async Task<string> GetLocations(string deliveryId)
        {
            return await RestClient.GetHttpResponse(baseUrl + deliveryId);
        }
        public async Task<string> GetCurrentLocation(string deliveryId)
        {

            return await RestClient.GetHttpResponse(baseUrl + deliveryId + droneStatusUrl);
        }

    }
}
