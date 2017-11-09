// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public interface INotifyMeRequestRepository
    {
        Task<IEnumerable<InternalNotifyMeRequest>> GetAllByDeliveryIdAsync(string deliveryId);
        Task AddAsync(InternalNotifyMeRequest notifyMeRequest);
    }
}