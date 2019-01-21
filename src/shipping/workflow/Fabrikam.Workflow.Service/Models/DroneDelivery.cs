// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.Workflow.Service.Models
{
    public class DroneDelivery
    {
        public string DeliveryId { get; set; }
        public Location Pickup { get; set; }
        public Location Dropoff { get; set; }
        public PackageDetail PackageDetail { get; set; }
        public bool Expedited { get; set; }
    }
}
