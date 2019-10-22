// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.Cosmos;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public class CosmosDBConnectionOptions
    {
        public string CosmosDBEndpoint { get; set; }

        public string CosmosDBKey { get; set; }

        public ConnectionMode CosmosDBConnectionMode { get; set; }

        public int CosmosDBMaxConnectionsLimit { get; set; }
    }
}
