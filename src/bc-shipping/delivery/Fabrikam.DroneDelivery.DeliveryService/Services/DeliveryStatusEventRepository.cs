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
    public class DeliveryStatusEventRepository : IDeliveryStatusEventRepository
    {
        public async Task AddAsync(DeliveryStatusEvent deliveryStatusEvent)
        {
            //FC: log with scope and timing
            await RedisCache<DeliveryStatusEvent>.CreateItemAsync(deliveryStatusEvent).ConfigureAwait(continueOnCapturedContext:false);
        }

        public async Task<ReadOnlyCollection<DeliveryStatusEvent>> GetByDeliveryIdAsync(string deliveryId)
        {
            var inflightDeliveryEvents = await RedisCache<DeliveryStatusEvent>.GetItemsAsync(Enum.GetNames(typeof(DeliveryEventType)).Select(n => $"{deliveryId}_{n}")).ConfigureAwait(continueOnCapturedContext: false);
            /// TODO: filter just the important events/milestones 
            return new ReadOnlyCollection<DeliveryStatusEvent>(inflightDeliveryEvents.ToList());
        }
    }
}
