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

namespace Fabrikam.DroneDelivery.DeliveryService.Hubs
{
    public class DroneHub : Hub
    {
        private readonly IDeliveryRepository deliveryRepository;
        private readonly INotifyMeRequestRepository notifyMeRequestRepository;
        private readonly INotificationService notificationService;
        private readonly IDeliveryTrackingEventRepository deliveryTrackingRepository;
        public DroneHub(IDeliveryRepository deliveryRepository,
                               INotifyMeRequestRepository notifyMeRequestRepository,
                               INotificationService notificationService,
                               IDeliveryTrackingEventRepository deliveryTrackingRepository
                              )
        {
            this.deliveryRepository = deliveryRepository;
            this.notifyMeRequestRepository = notifyMeRequestRepository;
            this.notificationService = notificationService;
            this.deliveryTrackingRepository = deliveryTrackingRepository;

        }
        public async Task GetDroneLocation(String deliveryId)
        {
            var delivery = await deliveryRepository.GetAsync(deliveryId);
            var latestDeliveryEvent = await this.deliveryTrackingRepository.GetLatestDeliveryEvent(deliveryId);
            if (delivery != null && latestDeliveryEvent != null)
            {
                var status = new DeliveryStatus(latestDeliveryEvent?.Stage ?? DeliveryStage.Created,
                                                latestDeliveryEvent?.Location ?? new Location(0, 0, 0),
                                                DateTime.Now.AddMinutes(10).ToString(),
                                                DateTime.Now.AddHours(1).ToString());
                await Clients.All.SendAsync("PositionUpdated", status);
            }
            else
                await Clients.All.SendAsync("PositionUpdated", "Not Found");
        }
    }
}
