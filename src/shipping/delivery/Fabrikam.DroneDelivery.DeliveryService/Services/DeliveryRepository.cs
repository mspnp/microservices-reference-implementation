// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public class DeliveryRepository : IDeliveryRepository
    {
        public async Task<InternalDelivery> GetAsync(string id)
        {
            return await RedisCache<InternalDelivery>.GetItemAsync(id).ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task CreateAsync(InternalDelivery delivery)
        {
            await RedisCache<InternalDelivery>.CreateItemAsync(delivery).ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task UpdateAsync(string id, InternalDelivery updatedDelivery)
        {
            await RedisCache<InternalDelivery>.UpdateItemAsync(id, updatedDelivery).ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task DeleteAsync(string id, InternalDelivery delivery)
        {
            await RedisCache<InternalDelivery>.DeleteItemAsync(id, delivery).ConfigureAwait(continueOnCapturedContext: false);
        }

        public Task<int> GetDeliveryCountAsync(string ownerId, int year, int month)
        {
            const int MinDeliveries = 1000;
            const int MaxDeliveries = 10000;

            var deliveryCount = new Random().Next(MinDeliveries, MaxDeliveries);

            return Task.FromResult(deliveryCount);
        }
    }
}
