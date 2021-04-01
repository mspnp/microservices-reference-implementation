// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public interface IDeliveryTrackingEventRepository
    {
        Task AddAsync(DeliveryTrackingEvent deliveryTrackingEvent);
        Task<ReadOnlyCollection<DeliveryTrackingEvent>> GetByDeliveryIdAsync(string deliveryId);
        Task<DeliveryTrackingEvent> GetLatestDeliveryEvent(string deliveryId);
    }
}