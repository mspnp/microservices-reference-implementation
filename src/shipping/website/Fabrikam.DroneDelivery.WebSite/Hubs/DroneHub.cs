using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.WebSite.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Fabrikam.DroneDelivery.WebSite.Hubs
{
    public class DroneHub : Hub
    {
        private IDroneManager _droneManager;
        public DroneHub(IDroneManager droneManager)
        {
            this._droneManager = droneManager;
        }
        public async Task GetDroneLocation(Guid deliveryId)
        {
            var droneLocation = await this._droneManager.GetDroneLocation(deliveryId);
            await Clients.All.SendAsync("PositionUpdated", droneLocation);
        }
    }
}
