using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.WebSite.Manager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Fabrikam.DroneDelivery.WebSite.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DroneController : ControllerBase
    {
       private DroneManager droneManager;
        public DroneController()
        {
            droneManager = new DroneManager();
        }

        /// <summary>
        /// Returns all location information of a specific drone by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AcceptVerbs("GET")]
        public async Task<string> GetAllLocations(string id)
        {
            return await droneManager.GetAllLocations(id);
        }

        /// <summary>
        /// Returns pickup/dropoff location information of a specific drone by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AcceptVerbs("GET")]
        public async Task<string> GetLocations(string id)
        {
            return await droneManager.GetLocations(id);
        }

        /// <summary>
        /// Returns current location information of a specific drone by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AcceptVerbs("GET")]
        public async Task<string> GetCurrentLocation(string id)
        {           
            return await droneManager.GetCurrentLocation(id);
        }
    }
}
