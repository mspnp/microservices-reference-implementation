using Fabrikam.DroneDelivery.ApiClient.Model;
using System;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.WebSite.Interfaces
{
    public interface IGeoCodeService
    {
        Task<BingMapsRESTToolkit.Location> ConvertAddressToLocation(string query);
    }
}
