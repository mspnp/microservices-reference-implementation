using BingMapsRESTToolkit;
using Fabrikam.DroneDelivery.ApiClient.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BingMapsRESTToolkit;
using Fabrikam.DroneDelivery.WebSite.Interfaces;

namespace Fabrikam.DroneDelivery.WebSite.Services
{
    public class GeoCodeService : IGeoCodeService
    {
        private IConfiguration configuration;

        public GeoCodeService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<BingMapsRESTToolkit.Location> ConvertAddressToLocation(string query)
        {
            var request = new GeocodeRequest()
            {
                Query = query,
                IncludeIso2 = true,
                IncludeNeighborhood = true,
                MaxResults = 1,
                BingMapsKey = this.configuration["BingMapKey"]
            };

            var response = await request.Execute();

            if (response != null &&
                response.ResourceSets != null &&
                response.ResourceSets.Length > 0 &&
                response.ResourceSets[0].Resources != null &&
                response.ResourceSets[0].Resources.Length > 0)
            {
                return response.ResourceSets[0].Resources[0] as BingMapsRESTToolkit.Location;
            }

            return null;
        }
    }
}
