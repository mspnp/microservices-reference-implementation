// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using Fabrikam.DroneDelivery.Common;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Models
{
    public class DroneDelivery
    {
        public string DeliveryId { get; set; }
        public Location Pickup { get; set; }
        public Location Dropoff { get; set; }
        public IEnumerable<PackageDetail> PackageDetails { get; set; }
        public bool Expedited { get; set; }
    }
}
