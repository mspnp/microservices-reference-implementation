// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Fabrikam.DroneDelivery.Common;
using Fabrikam.DroneDelivery.DeliveryService.Models;
using Fabrikam.DroneDelivery.DeliveryService.Services;

namespace Fabrikam.DroneDelivery.DeliveryService.Controllers
{
    [Route("api/[controller]")]
    public class DeliveriesController : Controller
    {
        private readonly IDeliveryRepository deliveryRepository;
        private readonly INotifyMeRequestRepository notifyMeRequestRepository;
        private readonly INotificationService notificationService;
        private readonly IDeliveryHistoryService deliveryHistoryService;
        private readonly IDeliveryStatusEventRepository deliveryStatusEventRepository;
        private readonly ILogger logger;

        public DeliveriesController(IDeliveryRepository deliveryRepository,
                                    INotifyMeRequestRepository notifyMeRequestRepository,
                                    INotificationService notificationService,
                                    IDeliveryHistoryService deliveryHistoryRepository,
                                    IDeliveryStatusEventRepository deliveryStatusEventRepository,
                                    ILoggerFactory loggerFactory)
        {
            this.deliveryRepository = deliveryRepository;
            this.notifyMeRequestRepository = notifyMeRequestRepository;
            this.notificationService = notificationService;
            this.deliveryHistoryService = deliveryHistoryRepository;
            this.deliveryStatusEventRepository = deliveryStatusEventRepository;
            this.logger = loggerFactory.CreateLogger<DeliveriesController>();
        }

        // GET api/deliveries/5
        [Route("/api/[controller]/{id}", Name = "GetDelivery")]
        [HttpGet]
        [ProducesResponseType(typeof(Delivery), 200)]
        public async Task<IActionResult> Get(string id)
        {
            logger.LogInformation("In Get action with id: {Id}", id);

            var internalDelivery = await deliveryRepository.GetAsync(id);

            if (internalDelivery == null) {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            return Ok(internalDelivery.ToExternal());
        }

        // GET api/deliveries/5/owner
        [Route("/api/[controller]/{id}/owner")]
        [HttpGet]
        [ProducesResponseType(typeof(UserAccount), 200)]
        public async Task<IActionResult> GetOwner(string id)
        {
            logger.LogInformation("In GetOwner action with id: {Id}", id);

            var delivery = await deliveryRepository.GetAsync(id);
            if (delivery == null)
            {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            return Ok(delivery.Owner);
        }

        // GET api/deliveries/5/status
        [Route("/api/[controller]/{id}/status")]
        [HttpGet]
        [ProducesResponseType(typeof(DeliveryStatus), 200)]
        public async Task<IActionResult> GetStatus(string id)
        {
            logger.LogInformation("In GetStatus action with id: {Id}", id);
            
            var delivery = await deliveryRepository.GetAsync(id);
            if (delivery == null)
            {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            var status = new DeliveryStatus(DeliveryEventType.InTransit, new Location(0,0,0), DateTime.Now.AddMinutes(10).ToString(), DateTime.Now.AddHours(1).ToString());
            return Ok(status);
        }

        // PUT api/deliveries/5
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Delivery), 201)]
        [ProducesResponseType(typeof(void), 204)]
        public async Task<IActionResult> Put([FromBody]Delivery delivery, string id)
        {
            logger.LogInformation("In Put action with delivery {Id}: {@DeliveryInfo}", id, delivery.ToLogInfo());

            try
            {
                var internalDelivery = delivery.ToInternal();

                // Adds the delivery created status event
                var deliveryStatusEvent = new DeliveryStatusEvent { DeliveryId = delivery.Id, Stage = DeliveryEventType.Created };
                await deliveryStatusEventRepository.AddAsync(deliveryStatusEvent);

                // Adds new inflight delivery 
                await deliveryRepository.CreateAsync(internalDelivery);

                return CreatedAtRoute("GetDelivery", new { id= delivery.Id }, delivery);
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

        // PATCH api/deliveries/5
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody]RescheduledDelivery rescheduledDelivery)
        {
            logger.LogInformation("In Patch action with id: {Id} and rescheduledDelivery: {@RescheduledDelivery}", id, rescheduledDelivery.ToLogInfo());

            var delivery = await deliveryRepository.GetAsync(id);
            if (delivery == null)
            {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            var updatedDelivery = new InternalDelivery(delivery.Id,
                                           delivery.Owner,
                                           rescheduledDelivery.Pickup,
                                           rescheduledDelivery.Dropoff,
                                           rescheduledDelivery.Deadline,
                                           delivery.Expedited,
                                           delivery.ConfirmationRequired,
                                           delivery.DroneId);
            
            // Adds the delivery rescheduled status event
            var deliveryStatusEvent = new DeliveryStatusEvent { DeliveryId = id, Stage = DeliveryEventType.Rescheduled };
            await deliveryStatusEventRepository.AddAsync(deliveryStatusEvent);

            // Updates the inflight delivery with updated information
            await deliveryRepository.UpdateAsync(id, updatedDelivery);
            return Ok();
        }

        // DELETE api/deliveries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            logger.LogInformation("In Delete action with id: {Id}", id);

            var delivery = await deliveryRepository.GetAsync(id);
            if (delivery == null)
            {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            // Adds the delivery cancelled status event
            var deliveryStatusEvent = new DeliveryStatusEvent { DeliveryId = id, Stage = DeliveryEventType.Cancelled };
            await deliveryStatusEventRepository.AddAsync(deliveryStatusEvent);

            // forwards cancelled delivery to the Delivery History
            var allEvents = await deliveryStatusEventRepository.GetByDeliveryIdAsync(id);
            await deliveryHistoryService.CancelAsync(delivery, allEvents.ToArray());

            // logical delivery deletion
            await deliveryRepository.DeleteAsync(id, delivery);

            return Ok();
        }

        // POST api/deliveries/5/notifymerequests
        [Route("/api/[controller]/{id}/notifymerequests")]
        [HttpPost]
        public async Task<IActionResult> NotifyMe(string id, [FromBody]NotifyMeRequest notifyMeRequest)
        {
            logger.LogInformation("In NotifyMe action with id: {Id} and notifyMeRequest: {@NotifyMeRequest}", id, notifyMeRequest.ToLogInfo());

            var delivery = await deliveryRepository.GetAsync(id);
            if (delivery == null)
            {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            //TODO: Authorize that user owns this delivery.

            var internalNotifyMeRequest = new InternalNotifyMeRequest
            {
                DeliveryId = id,
                EmailAddress = notifyMeRequest.EmailAddress,
                SMS = notifyMeRequest.SMS
            };

            await notifyMeRequestRepository.AddAsync(internalNotifyMeRequest);

            return NoContent();
        }

        /// <summary>
        /// This method will eventually be deprecated, replaced with code that listens to drone 
        /// events that signify a delivery confirmation.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="confirmation"></param>
        /// <returns></returns>
        // POST api/deliveries/5/confirmations
        [Route("/api/[controller]/{id}/confirmations")]
        [HttpPost]
        public async Task<IActionResult> Confirm(string id, [FromBody]Confirmation confirmation)
        {
            logger.LogInformation("In Confirm action with id: {Id} and confirmation: {@Confirmation}", id, confirmation.ToLogInfo());

            var delivery = await deliveryRepository.GetAsync(id);
            if (delivery == null)
            {
                logger.LogDebug("Delivery id: {Id} not found", id);
                return NotFound();
            }

            var confirmedDelivery = new InternalDelivery(delivery.Id,
                                           delivery.Owner,
                                           delivery.Pickup,
                                           confirmation.GeoCoordinates,
                                           delivery.Deadline,
                                           delivery.Expedited,
                                           delivery.ConfirmationRequired,
                                           delivery.DroneId);

            var internalConfirmation = new InternalConfirmation
            {
                DateTime = confirmation.DateTime,
                GeoCoordinates = confirmation.GeoCoordinates,
                ConfirmationType = confirmation.ConfirmationType,
                ConfirmationBlob = confirmation.ConfirmationBlob
            };

            // Adds the delivery complete status event
            await deliveryStatusEventRepository.AddAsync(new DeliveryStatusEvent
                                                            {
                                                                DeliveryId = id,
                                                                Stage = DeliveryEventType.DeliveryComplete
                                                            });
            // get all the milestones from cache
            var allEvents = await deliveryStatusEventRepository.GetByDeliveryIdAsync(id);

            // archives Delivery by sending it to the Delivery History + Confirmantion details as well as forwarding milestones to the Delivery History
            await deliveryHistoryService.CompleteAsync(confirmedDelivery, internalConfirmation, allEvents.ToArray());

            // sends notifications
            var notifyMeRequests = await notifyMeRequestRepository.GetAllByDeliveryIdAsync(id);
            IEnumerable<Task> notificationTasks = notifyMeRequests.Select(nR => notificationService.SendNotificationsAsync(nR));

            await Task.WhenAll(notificationTasks);

            // logical delivery deletion
            await deliveryRepository.DeleteAsync(id, confirmedDelivery);

            return Ok();
        }
    }
}
