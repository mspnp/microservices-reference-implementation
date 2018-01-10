// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class InternalNotifyMeRequest : BaseDocument
    {
        public string DeliveryId { get; set; }
        public string EmailAddress { get; set; }
        public string SMS { get; set; }
    }
}
