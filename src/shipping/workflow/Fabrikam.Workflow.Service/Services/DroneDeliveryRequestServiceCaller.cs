// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fabrikam.Workflow.Service.Models;
using Fabrikam.Workflow.Service.Utils;

namespace Fabrikam.Workflow.Service.Services
{
    public class DroneDeliveryRequestServiceCaller : IDroneDeliveryRequestServiceCaller
    {
        private readonly HttpClient _httpClient;

        public DroneDeliveryRequestServiceCaller(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
       
        public async Task<DeliverySchedule> ScheduleDeliveryAsync(Delivery deliveryRequest, string droneId)
        {
            try
            {
                var schedule = CreateDeliverySchedule(deliveryRequest, droneId);

                var response = await _httpClient.PutAsJsonAsync(schedule.Id, schedule);
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    return await response.Content.ReadAsAsync<DeliverySchedule>();
                }

                throw new BackendServiceCallFailedException(response.ReasonPhrase);
            }
            catch (BackendServiceCallFailedException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new BackendServiceCallFailedException(e.Message, e);
            }
        }

        private DeliverySchedule CreateDeliverySchedule(Delivery deliveryRequest, string droneId)
        {
            var pickup = LocationRandomizer.GetRandomLocation();
            var dropoff = LocationRandomizer.GetRandomLocation();

            if (deliveryRequest.PickupLocation.Contains(',') && deliveryRequest.DropoffLocation.Contains(','))
            {
                var pickupLat = Convert.ToDouble(deliveryRequest.PickupLocation.Split(',')[0].Trim());
                var pickupLong = Convert.ToDouble(deliveryRequest.PickupLocation.Split(',')[1].Trim());

                pickup = new Location()
                {
                    Altitude = 0,
                    Latitude = pickupLat,
                    Longitude = pickupLong
                };

                var dropOffLat = Convert.ToDouble(deliveryRequest.DropoffLocation.Split(',')[0].Trim());
                var dropOffLong = Convert.ToDouble(deliveryRequest.DropoffLocation.Split(',')[1].Trim());

                dropoff = new Location()
                {
                    Altitude = 0,
                    Latitude = dropOffLat,
                    Longitude = dropOffLong
                };
            }

            DeliverySchedule scheduleDelivery = new DeliverySchedule
            {
                Id = deliveryRequest.DeliveryId,
                Owner = new UserAccount { AccountId = Guid.NewGuid().ToString(), UserId = deliveryRequest.OwnerId },
                Pickup = pickup,
                Dropoff = dropoff,
                Deadline = deliveryRequest.Deadline,
                Expedited = deliveryRequest.Expedited,
                ConfirmationRequired = (ConfirmationType)deliveryRequest.ConfirmationRequired,
                DroneId = droneId,
            };

            return scheduleDelivery;
        }
    }
}
