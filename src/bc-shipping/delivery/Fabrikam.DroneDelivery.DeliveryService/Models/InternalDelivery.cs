// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.ObjectModel;
using Fabrikam.DroneDelivery.Common;
using Newtonsoft.Json;

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class InternalDelivery : BaseCache
    {
        public InternalDelivery(string id, 
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

        [JsonProperty(PropertyName = "id")]
        public string Id { get; }
        public UserAccount Owner { get; }
        public Location Pickup { get; }
        public Location Dropoff { get; }
        public string Deadline { get; }
        public bool Expedited { get; }
        public ConfirmationType ConfirmationRequired { get; }
        public string DroneId { get; }
        public override string Key => this.Id;
    }
}
