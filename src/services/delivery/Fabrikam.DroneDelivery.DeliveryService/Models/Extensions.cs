// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.DroneDelivery.Common;
using Newtonsoft.Json;

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public static class Extensions
    {
        public static Delivery ToExternal(this InternalDelivery delivery)
        {
            return new Delivery(delivery.Id,
                                delivery.Owner,
                                delivery.Pickup,
                                delivery.Dropoff,
                                delivery.Deadline,
                                delivery.Expedited,
                                delivery.ConfirmationRequired,
                                delivery.DroneId);
        }

        public static InternalDelivery ToInternal(this Delivery delivery)
        {
            return new InternalDelivery(delivery.Id,
                                        delivery.Owner,
                                        delivery.Pickup,
                                        delivery.Dropoff,
                                        delivery.Deadline,
                                        delivery.Expedited,
                                        delivery.ConfirmationRequired,
                                        delivery.DroneId);
        }

        //This method provides serialization with the option of obfuscating specific fields
        public static string ToLogInfo(this Delivery delivery)
        {
            var loggableDelivery = new Delivery(delivery.Id,
                                new UserAccount("user id for logging", delivery.Owner.AccountId), //Obfuscating UserId
                                delivery.Pickup,
                                delivery.Dropoff,
                                delivery.Deadline,
                                delivery.Expedited,
                                delivery.ConfirmationRequired,
                                delivery.DroneId);
            return JsonConvert.SerializeObject(loggableDelivery);
        }

        public static string ToLogInfo(this RescheduledDelivery rescheduledDelivery)
        {
            return JsonConvert.SerializeObject(rescheduledDelivery); //Nothing secure that needs to be obfuscated
        }

        public static string ToLogInfo(this NotifyMeRequest notifyMeRequest)
        {
            return JsonConvert.SerializeObject(notifyMeRequest); //Nothing secure that needs to be obfuscated
        }

        public static string ToLogInfo(this Confirmation confirmation)
        {
            return JsonConvert.SerializeObject(confirmation); //Nothing secure that needs to be obfuscated
        }
    }
}
