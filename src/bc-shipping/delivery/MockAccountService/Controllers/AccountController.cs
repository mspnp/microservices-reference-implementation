// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MockAccountService.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private readonly ILogger logger;

        public AccountController(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<AccountController>();
        }

        // GET api/account/{accountId}
        [HttpGet("{accountId}")]
        //TODO: [Authorize]
        public bool Get(string accountId)
        {
            logger.LogInformation("In Get action with accountId: {AccountId}", accountId);

            return true;
        }
    }
}
