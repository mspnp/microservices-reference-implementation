// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.DroneDelivery.Common;
using System;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Models
{
    public class DeliveryTrackingEvent : BaseCache
    {
        public string DeliveryId { get; set; }
        public DeliveryStage Stage { get; set; }
        public Location Location { get; set; }
        public override string Key => $"{this.DeliveryId}_{this.Created.Ticks}";
        public DateTimeOffset Created { get; set; }
    }
}