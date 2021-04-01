// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.DroneDelivery.Common;
using System;
using System.Collections.Generic;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Models
{
    public class DeliveryTrackingIds : BaseCache
    {
        public string DeliveryId { get; set; }
        public List<string> DeliveryKeys { get; set; }
        public override string Key => $"{this.DeliveryId}";
    }
}