﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.DroneDelivery.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Models
{
    public class DeliveryStatus
    {
        public DeliveryStatus(DeliveryStage deliveryStage, Location lastKnownLocation, string pickupeta, string deliveryeta)
        {
            Stage = deliveryStage;
            LastKnownLocation = lastKnownLocation;
            PickupETA = pickupeta;
            DeliveryETA = deliveryeta;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public DeliveryStage Stage { get; }

        public Location LastKnownLocation { get; }

        public string PickupETA { get; }

        public string DeliveryETA { get; }
    }
}