// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;
using Fabrikam.DroneDelivery.DroneSchedulerService.Services;
using Microsoft.AspNetCore.Http;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Controllers
{
    [Route("api/[controller]")]
    public class DroneDeliveriesController : Controller
    {
        private readonly ILogger logger;
        private readonly IInvoicingRepository _invoicingRepository;

        public DroneDeliveriesController(
               IInvoicingRepository invoicingRepository,
               ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<DroneDeliveriesController>();
            this._invoicingRepository = invoicingRepository;
        }

        // PUT api/dronedeliveries/5
        [HttpPut("{id}")]
        public string Put([FromBody]Models.DroneDelivery droneDelivery, string id)
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

        // GET api/dronedeliveries/utilization
        [HttpGet("utilization")]
        [ProducesResponseType(StatusCodes.Status200OK,
                              Type = typeof(DroneUtilization))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDroneUtilization([FromQuery] string ownerId, [FromQuery] int year, [FromQuery] int month)
        {
            // TODO: improve binding model --> improve err msg.
            if (string.IsNullOrEmpty(ownerId) 
                || year < 1 
                || month < 1)
            {
                return BadRequest();
            }

            var (traveledMiles, assignedHours) = await _invoicingRepository.
                GetAggreatedInvoincingDataAsync(ownerId, year, month);

            if (traveledMiles == 0d &&
                assignedHours == 0d)
            {
                return NotFound();
            }

            return Ok(new DroneUtilization
            {
                TraveledMiles = traveledMiles,
                AssignedHours = assignedHours
            });
        }
    }
}