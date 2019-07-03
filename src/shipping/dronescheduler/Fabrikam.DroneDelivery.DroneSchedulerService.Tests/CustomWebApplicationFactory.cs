// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Tests
{
    public class CustomWebApplicationFactory
        : WebApplicationFactory<Startup> 
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        { 
            builder
                .UseContentRoot(".")
                .UseEnvironment("Test")
                .ConfigureTestServices(s =>
                {
                    s.AddLogging(b => b.AddDebug());
                });

            base.ConfigureWebHost(builder);
        }
    }
}
