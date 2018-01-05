// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class NotifyMeRequest
    {
        public NotifyMeRequest(string emailaddress, string sms)
        {
            EmailAddress = emailaddress;
            SMS = sms;
        }

        public string EmailAddress { get; }
        public string SMS { get; }
    }
}
