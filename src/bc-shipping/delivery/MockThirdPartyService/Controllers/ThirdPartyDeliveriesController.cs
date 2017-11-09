// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Fabrikam.DroneDelivery.Common;

namespace MockThirdPartyService.Controllers
{
    [Route("api/[controller]")]
    public class ThirdPartyDeliveriesController : Controller
    {
        private readonly ILogger logger;

        public ThirdPartyDeliveriesController(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<ThirdPartyDeliveriesController>();
        }

        // PUT api/thirdpartydeliveries/5
        [HttpPut("{id}")]
        public IActionResult Put([FromBody]Location pickup, [FromBody]Location dropoff, string id)
        {
            logger.LogInformation("In Put action with DeliveryId: {DeliveryId}", id);

            return Ok(false);
        }
    }
}
