// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockDroneScheduler.Models;

namespace MockDroneScheduler.Controllers
{
    [Route("api/[controller]")]
    public class DroneDeliveriesController : Controller
    {
        private readonly ILogger logger;

        public DroneDeliveriesController(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<DroneDeliveriesController>();
        }

        // PUT api/dronedeliveries/5
        [HttpPut("{id}")]
        public string Put([FromBody]DroneDelivery droneDelivery, string id)
        {
            logger.LogInformation("In Put action with DeliveryId: {DeliveryId}", id);

            var guid = Guid.NewGuid();
            return $"AssignedDroneId{guid}";
        }

        // DELETE api/dronedeliveries/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            logger.LogInformation("In Delete action with DeliveryId: {DeliveryId}", id);
        }
    }
}
