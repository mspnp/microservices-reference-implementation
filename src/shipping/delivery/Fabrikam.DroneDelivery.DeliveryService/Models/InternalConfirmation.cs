// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.DroneDelivery.Common;

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class InternalConfirmation 
    {
        public DateTimeStamp DateTime { get; set; }
        public Location GeoCoordinates { get; set; }
        public ConfirmationType ConfirmationType { get; set; }
        public string ConfirmationBlob { get; set; }
    }
}
