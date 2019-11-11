// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Fabrikam.Workflow.Service.RequestProcessing;
using Fabrikam.Workflow.Service.Services;

namespace Fabrikam.Workflow.Service
{
    public static class ServiceStartup
    {
        private const string HealthCheckName = "ReadinessLiveness";
        private const string HealthCheckServiceAssembly = "Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckPublisherHostedService";

        public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddOptions();

            // Configure AppInsights
            services.AddApplicationInsightsKubernetesEnricher();
            services.AddApplicationInsightsTelemetry(context.Configuration);

            services.Configure<WorkflowServiceOptions>(context.Configuration);
            services.AddHostedService<WorkflowService>();

            services.AddTransient<IRequestProcessor, RequestProcessor>();

            // Add health check                                                                                                                                                                                                                     │
            services.AddHealthChecks().AddCheck(
                    HealthCheckName,
                    () => HealthCheckResult.Healthy("OK"));

            if (context.Configuration["HEALTHCHECK_INITIAL_DELAY"] is var configuredDelay &&
                double.TryParse(configuredDelay, out double delay))
            {
                services.Configure<HealthCheckPublisherOptions>(options =>
                    {
                        options.Delay = TimeSpan.FromMilliseconds(delay);
                    });
            }

            services
                .AddHttpClient<IPackageServiceCaller, PackageServiceCaller>(c =>
                {
                    c.BaseAddress = new Uri(context.Configuration["SERVICE_URI_PACKAGE"]);
                })
                .AddResiliencyPolicies(context.Configuration);

            services
                .AddHttpClient<IDroneSchedulerServiceCaller, DroneSchedulerServiceCaller>(c =>
                {
                    c.BaseAddress = new Uri(context.Configuration["SERVICE_URI_DRONE"]);
                })
                .AddResiliencyPolicies(context.Configuration);

            services
                .AddHttpClient<IDeliveryServiceCaller, DeliveryServiceCaller>(c =>
                {
                    c.BaseAddress = new Uri(context.Configuration["SERVICE_URI_DELIVERY"]);
                })
                .AddResiliencyPolicies(context.Configuration);

            // workaround .NET Core 2.2: for more info https://github.com/aspnet/AspNetCore.Docs/blob/master/aspnetcore/host-and-deploy/health-checks/samples/2.x/HealthChecksSample/LivenessProbeStartup.cs#L51
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton(typeof(IHostedService),
                    typeof(HealthCheckPublisherOptions).Assembly
                        .GetType(HealthCheckServiceAssembly)));

            services.AddSingleton<IHealthCheckPublisher, ReadinessLivenessPublisher>();
        }
    }
}
