// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.DroneDelivery.Common;

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class DeliveryStatusEvent : BaseCache
    {
        public string DeliveryId { get; set; }
        public DeliveryEventType Stage { get; set; }
        public Location Location { get; set; }
        public override string Key => $"{this.DeliveryId}_{this.Stage.ToString()}";
    }
}