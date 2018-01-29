// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
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
        }

        public async Task<ReadOnlyCollection<DeliveryTrackingEvent>> GetByDeliveryIdAsync(string deliveryId)
        {
            var inflightDeliveryTrackingEvents = await RedisCache<DeliveryTrackingEvent>.GetItemsAsync(Enum.GetNames(typeof(DeliveryStage)).Select(n => $"{deliveryId}_{n}")).ConfigureAwait(continueOnCapturedContext: false);
            /// TODO: filter just the important events/milestones 
            return new ReadOnlyCollection<DeliveryTrackingEvent>(inflightDeliveryTrackingEvents.ToList());
        }
    }
}
