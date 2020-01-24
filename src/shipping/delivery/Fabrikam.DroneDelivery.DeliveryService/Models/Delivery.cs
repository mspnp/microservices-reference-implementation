// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.DroneDelivery.Common;

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class Delivery
    {
        public Delivery() {}

        public Delivery(string id, 
                        UserAccount owner, 
                        Location pickup, 
                        Location dropoff, 
                        string deadline, 
                        bool expedited, 
                        ConfirmationType confirmationRequired,
                        string droneid)
        {
            Id = id;
            Owner = owner;
            Pickup = pickup;
            Dropoff = dropoff;
            Deadline = deadline;
            Expedited = expedited;
            ConfirmationRequired = confirmationRequired;
            DroneId = droneid;
        }

        public string Id { get; set;}
        public UserAccount Owner { get; set;}
        public Location Pickup { get; set;}
        public Location Dropoff { get; set;}
        public string Deadline { get; set;}
        public bool Expedited { get; set;}
        public ConfirmationType ConfirmationRequired { get; set;}
        public string DroneId { get; set;}
    }
}
