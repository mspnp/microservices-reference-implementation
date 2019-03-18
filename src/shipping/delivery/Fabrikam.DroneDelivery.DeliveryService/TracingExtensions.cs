// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace Fabrikam.DroneDelivery.DeliveryService
{
    public static class TracingExtensions
    {
        public static IServiceCollection AddApplicationInsightsKubernetesEnricher (this IServiceCollection services)
        {
            services.Configure<TelemetryConfiguration>(
                (config) =>
                    config.AddApplicationInsightsKubernetesEnricher(
                            applyOptions: null)
            );

            return services;
        }
    }
}
