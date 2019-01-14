// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fabrikam.Workflow.Service.RequestProcessing;
using Fabrikam.Workflow.Service.Services;

namespace Fabrikam.Workflow.Service
{
    public static class ServiceStartup
    {
        public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddOptions();

            services.Configure<WorkflowServiceOptions>(context.Configuration);
            services.AddHostedService<WorkflowService>();

            services.AddTransient<IRequestProcessor, RequestProcessor>();

            services.AddHttpClient<IPackageServiceCaller, PackageServiceCaller>(c =>
            {
                c.BaseAddress = new Uri(context.Configuration["SERVICE_URI_PACKAGE"]);
            });
        }
    }
}
