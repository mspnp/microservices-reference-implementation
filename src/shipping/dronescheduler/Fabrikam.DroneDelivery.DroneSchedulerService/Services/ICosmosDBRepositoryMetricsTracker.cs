// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.Cosmos;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public interface ICosmosDBRepositoryMetricsTracker<T>
         where T : BaseDocument
    {
        ICosmosDBRepositoryQueryMetricsTracker<T> GetQueryMetricsTracker(
            string collection,
            string partitionKey,
            int maxParallelism,
            int maxConnections,
            ConnectionMode connectionMode,
            int maxBufferedItemCount);
    }
}
