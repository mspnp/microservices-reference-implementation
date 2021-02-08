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
using Microsoft.Extensions.Configuration;

namespace Fabrikam.Workflow.Service.Services
{
    public class DroneSimulator : IDroneSimulator
    {
        private readonly HttpClient _httpClient;
        private readonly string trackingUrl;//= "";

        public DroneSimulator(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            trackingUrl = configuration.GetValue<string>("DeliveryTrackingUrl");
        }

        public async Task Simulate(string deliveryId)
        {
            try
            {

                var response = await _httpClient.PutAsync($"?trackingUrl={trackingUrl}&deliveryId={deliveryId}", null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    await response.Content.ReadAsStringAsync();
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
    }
}
