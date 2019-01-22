// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.Workflow.Service.Models
{
    public class DeliverySchedule
    {
        public string Id { get; set; }
        public UserAccount Owner { get; set; }
        public Location Pickup { get; set; }
        public Location Dropoff { get; set; }
        public string Deadline { get; set; }
        public bool Expedited { get; set; }
        public ConfirmationType ConfirmationRequired { get; set; }
        public string DroneId { get; set; }
    }
}
