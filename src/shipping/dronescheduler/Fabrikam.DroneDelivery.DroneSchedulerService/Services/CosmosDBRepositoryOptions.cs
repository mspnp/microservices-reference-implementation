// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.Cosmos;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public class CosmosDBRepositoryOptions<T>
        where T : BaseDocument
    {
        public string DatabaseId { get; set; }

        public string CollectionId { get; set; }

        public Container Container { get; set; }

        public int MaxParallelism { get; set; } = -1;

        public int MaxBufferedItemCount { get; set; }
    }
}
