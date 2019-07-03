// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public interface IDeliveryRepository
    {
        Task<InternalDelivery> GetAsync(string id);
        Task CreateAsync(InternalDelivery delivery);
        Task UpdateAsync(string id, InternalDelivery updatedDelivery);
        Task DeleteAsync(string id, InternalDelivery delivery);
        Task<int> GetDeliveryCountAsync(string ownerId, int year, int month);
    }
}