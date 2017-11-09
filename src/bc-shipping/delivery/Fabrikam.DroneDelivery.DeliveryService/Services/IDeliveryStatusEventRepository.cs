// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public interface IDeliveryStatusEventRepository
    {
        Task AddAsync(DeliveryStatusEvent deliveryStatusEvent);
        Task<ReadOnlyCollection<DeliveryStatusEvent>> GetByDeliveryIdAsync(string deliveryId);
    }
}