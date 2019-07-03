// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Models
{
    public class BaseDocument
    {
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; internal set; }

        [JsonProperty(PropertyName = "type")]
        public string DocumentType { get; internal set; }
    }
}
