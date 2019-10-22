// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Fabrikam.DroneDelivery.DroneSchedulerService.Tests")]
namespace Fabrikam.DroneDelivery.DroneSchedulerService.Models
{
    public class InternalDroneUtilization: BaseDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; internal set; }

        [JsonProperty(PropertyName = "year")]
        public int Year { get; internal set; }

        [JsonProperty(PropertyName = "month")]
        public int Month { get; internal set; }

        [JsonProperty(PropertyName = "ownerId")]
        public string OwnerId { get; internal set; }

        [JsonProperty(PropertyName = "travelledMiles")]
        public double TraveledMiles { get; internal set; }

        [JsonProperty(PropertyName = "assignedHours")]
        public double AssignedHours { get; internal set; }
    }
}
