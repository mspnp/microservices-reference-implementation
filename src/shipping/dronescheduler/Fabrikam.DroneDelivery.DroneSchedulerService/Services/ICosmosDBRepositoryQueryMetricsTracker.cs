// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Azure.Cosmos;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public interface ICosmosDBRepositoryQueryMetricsTracker<T> : IDisposable
         where T : BaseDocument
    {
        void TrackResponseMetrics(FeedResponse<T> response);
    }
}
