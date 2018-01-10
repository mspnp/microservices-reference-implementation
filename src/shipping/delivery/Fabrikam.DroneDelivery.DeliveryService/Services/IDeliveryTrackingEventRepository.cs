// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public interface IDeliveryTrackingEventRepository
    {
        Task AddAsync(DeliveryTrackingEvent deliveryTrackingEvent);
        Task<ReadOnlyCollection<DeliveryTrackingEvent>> GetByDeliveryIdAsync(string deliveryId);
    }
}