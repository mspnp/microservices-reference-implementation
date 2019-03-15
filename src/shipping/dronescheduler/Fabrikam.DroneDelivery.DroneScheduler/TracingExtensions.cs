// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace MockDroneScheduler
{
    public static class TracingExtensions
    {
        private const string AppInsightsSectionName = "ApplicationInsightsLogger";

        public static IServiceCollection AddApplicationInsightsKubernetesEnricher (this IServiceCollection services)
        {
            services.Configure<TelemetryConfiguration>(
                (config) =>
                    config.AddApplicationInsightsKubernetesEnricher(
                            applyOptions: null)
            );

            return services;
        }

        public static ILoggingBuilder AddApplicationInsights(
            this ILoggingBuilder loggingBuilder,
            IConfiguration configuration)
        {
            var aiOptions =
                    configuration
                        .GetSection(AppInsightsSectionName)
                        ?.Get<ApplicationInsightsLoggerExtendedOptions>() ??
                        new ApplicationInsightsLoggerExtendedOptions();

            loggingBuilder.AddFilter
                    <ApplicationInsightsLoggerProvider>(
                        "",
                        aiOptions.TelemetryLogLevel);

            loggingBuilder.AddApplicationInsights(
                o =>
                {
                    o.IncludeScopes =
                        aiOptions.IncludeScopes;
                    o.TrackExceptionsAsExceptionTelemetry =
                        aiOptions.TrackExceptionsAsExceptionTelemetry;
                });

            return loggingBuilder;
        }

        private class ApplicationInsightsLoggerExtendedOptions
            : ApplicationInsightsLoggerOptions
        {
            public ApplicationInsightsLoggerExtendedOptions()
                : base()
            {
                this.TelemetryLogLevel = LogLevel.Warning;
            }

            public LogLevel TelemetryLogLevel
            { get; set; }
        }
    }
}
