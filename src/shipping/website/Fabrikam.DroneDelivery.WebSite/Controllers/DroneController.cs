using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.ApiClient;
using Fabrikam.DroneDelivery.ApiClient.Model;
using Fabrikam.DroneDelivery.WebSite.Accessors;
using Fabrikam.DroneDelivery.WebSite.Interfaces;
using Fabrikam.DroneDelivery.WebSite.Manager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fabrikam.DroneDelivery.WebSite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DroneController : ControllerBase
    {
        private const string apiUrl = "https://localhost:44322";

        private IDroneManager _droneManager;

        public DroneController()
        {
            //IDroneManager droneManager
            this._droneManager = new DroneManager(new TrackingAccessor(new TrackingClient(apiUrl)));
        }

        /// <summary>
        /// Returns the delivery 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{deliveryId}")]
        public async Task<Delivery> GetDelivery(Guid deliveryId)
        {
            return await this._droneManager.GetDelivery(deliveryId);
        }

        /// <summary>
        /// Returns current location of the drone
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{deliveryId}/dronelocation")]
        public async Task<DroneLocation> GetDroneLocation(Guid deliveryId)
        {
            return await this._droneManager.GetDroneLocation(deliveryId);
        }
    }
}
