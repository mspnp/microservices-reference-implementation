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
    public class DroneSchedulerServiceCaller : IDroneSchedulerServiceCaller
    {
        private readonly HttpClient _httpClient;

        public DroneSchedulerServiceCaller(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetDroneIdAsync(Delivery deliveryRequest)
        {
            try
            {
                var delivery = CreateDroneDelivery(deliveryRequest);

                var response = await _httpClient.PutAsJsonAsync($"{delivery.DeliveryId}", delivery);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await response.Content.ReadAsStringAsync();
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

        private DroneDelivery CreateDroneDelivery(Delivery deliveryRequest)
        {
            DroneDelivery delivery = new DroneDelivery();
            delivery.DeliveryId = deliveryRequest.DeliveryId;

            delivery.Dropoff = LocationRandomizer.GetRandomLocation();
            delivery.Pickup = LocationRandomizer.GetRandomLocation();

            delivery.Expedited = delivery.Expedited;
            delivery.PackageDetail = ModelsConverter.GetPackageDetail(deliveryRequest.PackageInfo);

            return delivery;
        }
    }
}
