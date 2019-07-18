// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Models
{
    public class DroneUtilization
    {
        public double TraveledMiles { get; internal set; }
        public double AssignedHours { get; internal set; }
    }
}
