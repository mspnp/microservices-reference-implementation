// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.Common;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public class DeliveryTrackingRepository : IDeliveryTrackingEventRepository
    {
        public async Task AddAsync(DeliveryTrackingEvent deliveryTrackingEvent)
        {
            //FC: log with scope and timing
            await RedisCache<DeliveryTrackingEvent>.CreateItemAsync(deliveryTrackingEvent).ConfigureAwait(continueOnCapturedContext:false);

            var trackingIds = await RedisCache<DeliveryTrackingIds>.GetItemAsync(deliveryTrackingEvent.DeliveryId).ConfigureAwait(continueOnCapturedContext: false);
            if (trackingIds == null)
            {
                trackingIds = new DeliveryTrackingIds()
                {
                    DeliveryId = deliveryTrackingEvent.DeliveryId,
                    DeliveryKeys = new List<string>()
                };
            }

            trackingIds.DeliveryKeys.Add(deliveryTrackingEvent.Key);

            await RedisCache<DeliveryTrackingIds>.UpdateItemAsync(deliveryTrackingEvent.DeliveryId, trackingIds);
        }

        public async Task<ReadOnlyCollection<DeliveryTrackingEvent>> GetByDeliveryIdAsync(string deliveryId)
        {
            var inflightDeliveryTrackingEvents = await RedisCache<DeliveryTrackingEvent>.GetItemsAsync(Enum.GetNames(typeof(DeliveryStage)).Select(n => $"{deliveryId}_{n}")).ConfigureAwait(continueOnCapturedContext: false);
            /// TODO: filter just the important events/milestones 
            return new ReadOnlyCollection<DeliveryTrackingEvent>(inflightDeliveryTrackingEvents.ToList());
        }

        public async Task<DeliveryTrackingEvent> GetLatestDeliveryEvent(string deliveryId)
        {
            var trackingIds = await RedisCache<DeliveryTrackingIds>.GetItemAsync(deliveryId).ConfigureAwait(continueOnCapturedContext: false); ;

            var trackingEvents = new List<DeliveryTrackingEvent>();
            foreach (var deliveryKey in trackingIds.DeliveryKeys)
            {
                var deliveryTrackingEvent = await RedisCache<DeliveryTrackingEvent>.GetItemAsync(deliveryKey).ConfigureAwait(continueOnCapturedContext: false); ;
                trackingEvents.Add(deliveryTrackingEvent);
            }

            var latestDeliveryEvent = trackingEvents?.OrderByDescending(e => e.Created)?.FirstOrDefault();
            return latestDeliveryEvent;
        }
    }
}
