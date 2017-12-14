// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.DeliveryService.Models;
using Fabrikam.DroneDelivery.Common;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public class DeliveryHistoryService : IDeliveryHistoryService
    {
        public async Task CompleteAsync(InternalDelivery delivery, InternalConfirmation confirmation, params DeliveryTrackingEvent[] deliveryTrackingEvents)
        {
            //TODO: shallowing confirmation (TBD)
            await EventHubSender<DeliveryHistory>.SendMessageAsync(new DeliveryHistory(delivery.Id, delivery, deliveryTrackingEvents), nameof(DeliveryStage.Completed), delivery.Id.Substring(0, Constants.PartitionKeyLength)).ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task CancelAsync(InternalDelivery delivery, params DeliveryTrackingEvent[] deliveryTrackingEvents)
        {
            await EventHubSender<DeliveryHistory>.SendMessageAsync(new DeliveryHistory(delivery.Id, delivery, deliveryTrackingEvents), nameof(DeliveryStage.Cancelled), delivery.Id.Substring(0, Constants.PartitionKeyLength)).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}