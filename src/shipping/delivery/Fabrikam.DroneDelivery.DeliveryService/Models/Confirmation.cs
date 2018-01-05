// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.DroneDelivery.Common;

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class Confirmation
    {
        public Confirmation(DateTimeStamp dateTime, Location geoCoordinates, ConfirmationType confirmationType, string confirmationBlob)
        {
            DateTime = dateTime;
            GeoCoordinates = geoCoordinates;
            ConfirmationType = confirmationType;
            ConfirmationBlob = confirmationBlob;
        }
        public DateTimeStamp DateTime { get; }
        public Location GeoCoordinates { get; }
        public ConfirmationType ConfirmationType { get; }
        public string ConfirmationBlob { get; }
    }
}
