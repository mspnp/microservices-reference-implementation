// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class DeliveryHistory : BaseMessage
    {
        public DeliveryHistory(string id, 
                        InternalDelivery delivery,
                        params DeliveryTrackingEvent[] deliveryTrackingEvents)
        {
            Id = id;
            Delivery = delivery;
            DeliveryTrackingEvents = deliveryTrackingEvents;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; }
        public InternalDelivery Delivery { get; }
        public DeliveryTrackingEvent[] DeliveryTrackingEvents { get; }
    }
}
