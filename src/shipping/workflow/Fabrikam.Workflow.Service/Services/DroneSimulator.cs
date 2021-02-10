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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fabrikam.Workflow.Service.Services
{
    public class DroneSimulator : IDroneSimulator
    {
        private readonly HttpClient _httpClient;
        private readonly string trackingUrl;//= "";
        private readonly ILogger<DroneSimulator> _logger;

        public DroneSimulator(HttpClient httpClient, IConfiguration configuration, ILogger<DroneSimulator> logger)
        {
            _httpClient = httpClient;
            trackingUrl = configuration.GetValue<string>("DeliveryTrackingUrl");
            _logger = logger;
        }

        public async Task Simulate(string deliveryId)
        {
            try
            {
                _logger.LogInformation($" Making Request to Simulator for deliveryId: {deliveryId}");
                StringContent queryString = new StringContent($"?trackingUrl={trackingUrl}&deliveryId={deliveryId}");

                var response = await _httpClient.PutAsync(_httpClient.BaseAddress, queryString);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _logger.LogInformation($" Reading respone from Simulator for deliveryId: {deliveryId}");
                    var result = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($" Response from Simulator for deliveryId: {deliveryId} is {result}");
                }
                else
                {
                    _logger.LogInformation($" Exception thrown from else block for deliveryId {deliveryId } ");
                    throw new BackendServiceCallFailedException(response.ReasonPhrase);
                }
            }
            catch (AggregateException ex)
            {
                _logger.LogError(ex, ex.Message + $" Exception thrown from non General catch block for deliveryId {deliveryId } ");
                throw;
            }
            catch (BackendServiceCallFailedException)
            {
                _logger.LogError($" Exception thrown from non General catch block for deliveryId {deliveryId } ");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($" Exception thrown from  General catch block for deliveryId {deliveryId } ");
                throw new BackendServiceCallFailedException(e.Message, e);
            }
        }
    }
}
