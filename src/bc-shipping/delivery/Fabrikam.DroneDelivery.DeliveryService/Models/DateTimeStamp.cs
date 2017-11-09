// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class DateTimeStamp
    {
        public DateTimeStamp(string dateTimeValue)
        {
            DateTimeValue = dateTimeValue;
        }

        public string DateTimeValue { get; set; }
    }
}
