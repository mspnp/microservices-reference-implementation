// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text;
using System;
using Microsoft.Extensions.Logging;

namespace MockDeliveryScheduler.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfigurationRoot _configuration;
        private readonly ILogger _logger;

        public HomeController(IConfigurationRoot configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<HomeController>();
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("In Index action!!!");

            var guid = Guid.NewGuid();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("l5d-ctx-trace", $"TestCorrelationId{guid}");

            var deliveryId = $"deliveryId{guid}";
            var droneDelivery = @"{""deliveryId"": """ + deliveryId + @""",  ""pickup"": {""altitude"": 1, ""latitude"": 2, ""longitude"": 3}, ""dropoff"": {""altitude"": 3, ""latitude"": 2, ""longitude"": 1}, ""packageDetails"": [{""id"": ""packageId"", ""size"": 0}], ""expedited"": true}";
            var response = await client.PutAsync($"http://dronescheduler/api/dronedeliveries/{deliveryId}" , new StringContent(droneDelivery, Encoding.UTF8, "application/json"));
            var droneId = await response.Content.ReadAsStringAsync();
            ViewBag.Message1 = droneId;

            var delivery = @"{
              ""id"": """ + deliveryId + @""",
              ""owner"": {
                            ""userId"": ""user123"",
                            ""accountId"": ""account123""
              },
              ""pickup"": {
                            ""altitude"": 1,
                            ""latitude"": 2,
                            ""longitude"": 3
              },
              ""dropoff"": {
                            ""altitude"": 3,
                            ""latitude"": 2,
                            ""longitude"": 1
              },
              ""packageIds"": [
                ""packageId1"", ""packageId2""
              ],
              ""deadline"": ""deadline"",
              ""expedited"": true,
              ""droneId"": """ + droneId + @"""
            }";
            response = await client.PutAsync($"http://delivery/api/deliveries/{deliveryId}", new StringContent(delivery, Encoding.UTF8, "application/json"));
            ViewBag.Message2 = await response.Content.ReadAsStringAsync();

            return View();
        }
        
        public IActionResult Error()
        {
            return View();
        }
    }
}
