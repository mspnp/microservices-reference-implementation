using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fabrikam.DroneDelivery.Common;
using Fabrikam.DroneDelivery.DeliveryService.Models;
using Fabrikam.DroneDelivery.DeliveryService.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Fabrikam.DroneDelivery.DeliveryService.Interfaces;
using Microsoft.AspNetCore.Cors;

namespace Fabrikam.DroneDelivery.DeliveryService.Hubs
{
    [EnableCors("CorsPolicy")]
    public class DroneHub : Hub<ITrackingClient>
    {
        private readonly IDeliveryRepository deliveryRepository;
        private readonly IDeliveryTrackingEventRepository deliveryTrackingRepository;

        public DroneHub(IDeliveryRepository deliveryRepository,
                        IDeliveryTrackingEventRepository deliveryTrackingRepository)
        {
            this.deliveryRepository = deliveryRepository;
            this.deliveryTrackingRepository = deliveryTrackingRepository;
        }

        public async Task Subscribe(string deliveryId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, deliveryId);
        }

        public async Task SendLocation(string deliveryId)
        {
            var delivery = await deliveryRepository.GetAsync(deliveryId);
            var latestDeliveryEvent = await this.deliveryTrackingRepository.GetLatestDeliveryEvent(deliveryId);
            if (delivery != null && latestDeliveryEvent != null)
            {
                var status = new DeliveryStatus(latestDeliveryEvent?.Stage ?? DeliveryStage.Created,
                                                latestDeliveryEvent?.Location ?? new Location(0, 0, 0),
                                                DateTime.Now.AddMinutes(10).ToString(),
                                                DateTime.Now.AddHours(1).ToString());

                await Clients.Group(deliveryId).RecieveLocation(status);
            }
            else 
            { 
                await Clients.Group(deliveryId).RecieveLocation(null);
            }
        }
    }
}
