// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.ObjectModel;
using Fabrikam.DroneDelivery.Common;

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class RescheduledDelivery
    {
        public RescheduledDelivery(Location pickup, Location dropoff, string deadline)
        {
            Pickup = pickup;
            Dropoff = dropoff;
            Deadline = deadline;
        }

        public Location Pickup { get; }
        public Location Dropoff { get; }
        public string Deadline { get; }
    }
}
