// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Fabrikam.DroneDelivery.Common;
using Fabrikam.DroneDelivery.DeliveryService.Models;
using Fabrikam.DroneDelivery.DeliveryService.Services;

namespace Fabrikam.DroneDelivery.DeliveryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DroneController : ControllerBase
    {
        private readonly IDeliveryRepository deliveryRepository;
        private readonly INotifyMeRequestRepository notifyMeRequestRepository;
        private readonly INotificationService notificationService;
        private readonly IDeliveryTrackingEventRepository deliveryTrackingRepository;
        private readonly ILogger logger;

        public DroneController(IDeliveryRepository deliveryRepository,
                               INotifyMeRequestRepository notifyMeRequestRepository,
                               INotificationService notificationService,
                               IDeliveryTrackingEventRepository deliveryTrackingRepository,
                               ILoggerFactory loggerFactory)
        {
            this.deliveryRepository = deliveryRepository;
            this.notifyMeRequestRepository = notifyMeRequestRepository;
            this.notificationService = notificationService;
            this.deliveryTrackingRepository = deliveryTrackingRepository;
            this.logger = loggerFactory.CreateLogger<DeliveriesController>();
        }

        // GET api/drone/5/location
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DeliveryStatus), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLocation(string id)
        {
            logger.LogInformation("In GetLocation action with id: {Id}", id);

            var delivery = await deliveryRepository.GetAsync(id);
            if (delivery == null)
            {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            var latestDeliveryEvent = await this.deliveryTrackingRepository.GetLatestDeliveryEvent(id);
            if (latestDeliveryEvent == null) return NotFound();

            var status = new DeliveryStatus(latestDeliveryEvent?.Stage ?? DeliveryStage.Created,
                                            latestDeliveryEvent?.Location ?? new Location(0, 0, 0), 
                                            DateTime.Now.AddMinutes(10).ToString(), 
                                            DateTime.Now.AddHours(1).ToString());            
            return Ok(status);
        }

        // PUT api/drone/5
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Put([FromBody]DeliveryTracking deliveryTracking, string id)
        {
            logger.LogInformation("In Put action with delivery {Id}: {@deliveryTracking}", id, deliveryTracking.ToLogInfo());

            var delivery = await deliveryRepository.GetAsync(id);
            if (delivery == null)
            {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            await this.deliveryTrackingRepository.AddAsync(new DeliveryTrackingEvent() { 
                DeliveryId = deliveryTracking.DeliveryId,
                Location = deliveryTracking.Location,
                Stage = deliveryTracking.Stage,
                Created = DateTimeOffset.UtcNow
            });

            return Ok();
        }

    }
}
