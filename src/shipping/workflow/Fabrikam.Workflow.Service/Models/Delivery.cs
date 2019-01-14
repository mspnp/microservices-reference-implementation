// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Fabrikam.Workflow.Service.Models
{
    public class Delivery
    {
        public string DeliveryId { get; set; }
        public string OwnerId { get; set; }
        public string PickupLocation { get; set; }
        public string DropoffLocation { get; set; }
        public string Deadline { get; set; }
        public bool Expedited { get; set; }
        public ConfirmationRequired ConfirmationRequired { get; set; }
        public DateTime PickupTime { get; set; }
        public PackageInfo PackageInfo { get; set; }
    }
}
