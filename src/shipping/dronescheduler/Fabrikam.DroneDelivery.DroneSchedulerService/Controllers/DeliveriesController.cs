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
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;
using Fabrikam.DroneDelivery.DroneSchedulerService.Services;


namespace Fabrikam.DroneDelivery.DeliveryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveriesController : ControllerBase
    {
        private readonly IDeliveryRepository deliveryRepository;
        private readonly IDeliveryTrackingEventRepository deliveryTrackingRepository;
        private readonly ILogger logger;

        public DeliveriesController(IDeliveryRepository deliveryRepository,                                    
                                    IDeliveryTrackingEventRepository deliveryTrackingRepository,
                                    ILoggerFactory loggerFactory)
        {
            this.deliveryRepository = deliveryRepository;            
            this.deliveryTrackingRepository = deliveryTrackingRepository;
            this.logger = loggerFactory.CreateLogger<DeliveriesController>();
        }

        // GET api/deliveries/5
        [HttpGet("{id}", Name = "GetDelivery")]
        [HttpGet("public/{id}")]
        [ProducesResponseType(typeof(Delivery), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get(string id)
        {
            logger.LogInformation("In Get action with id: {Id}", id);

            var internalDelivery = await deliveryRepository.GetAsync(id);

            if (internalDelivery == null)
            {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            return Ok(internalDelivery.ToExternal());
        }

        // PUT api/deliveries/5
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Delivery), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Put([FromBody]Delivery delivery, string id)
        {
            logger.LogInformation("In Put action with delivery {Id}: {@DeliveryInfo}", id, delivery.ToLogInfo());

            try
            {
                var internalDelivery = delivery.ToInternal();

                // Adds new inflight delivery 
                await deliveryRepository.CreateAsync(internalDelivery);

                // Adds the delivery created status event
                var deliveryTrackingEvent = new DeliveryTrackingEvent { DeliveryId = delivery.Id, Stage = DeliveryStage.Created };
                await deliveryTrackingRepository.AddAsync(deliveryTrackingEvent);

                return CreatedAtRoute("GetDelivery", new { id = delivery.Id }, delivery);
            }
            catch (DuplicateResourceException)
            {
                //This method is mainly used to create deliveries. If the delivery already exists then update
                logger.LogInformation("Updating resource with delivery id: {DeliveryId}", id);

                var internalDelivery = delivery.ToInternal();

                // Updates inflight delivery 
                await deliveryRepository.UpdateAsync(id, internalDelivery);

                return NoContent();
            }
        }       
    }
}
