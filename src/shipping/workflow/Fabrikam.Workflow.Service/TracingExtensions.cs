// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fabrikam.Workflow.Service
{
    /// <summary>
    /// Application Insights setup class based on https://docs.microsoft.com/en-us/azure/azure-monitor/app/console
    /// </summary>
    /// <remarks>
    /// Telemetry Modules initialization as expected based on https://github.com/Microsoft/ApplicationInsights-aspnetcore/blob/04b5485d4a8aa498b2d99c60bdf8ca59bc9103fc/src/Microsoft.ApplicationInsights.AspNetCore/Implementation/TelemetryConfigurationOptions.cs#L27
    /// </remarks>
    internal static class TracingExtensions
    {
        private const string CustomKeyVaultAIppInsightsIKey = "ApplicationInsights-InstrumentationKey";

        public static IServiceCollection AddApplicationInsightsTelemetry(
          this IServiceCollection services,
          IConfiguration configuration)
        {
            services.AddSingleton(s =>
            {
                var telemetryConfig = TelemetryConfiguration.CreateDefault();

                var config = s.GetService<IConfiguration>();
                var instrumentationKey = config[CustomKeyVaultAIppInsightsIKey];

                if (!string.IsNullOrWhiteSpace(instrumentationKey))
                {
                    telemetryConfig.InstrumentationKey = instrumentationKey;
                }

                // use processors
                telemetryConfig
                    .DefaultTelemetrySink
                    .TelemetryProcessorChainBuilder
                    .Use((next) =>
                {
                    var processor = new QuickPulseTelemetryProcessor(next);

                    var quickPulseModule = s.GetServices<ITelemetryModule>()
                                        .OfType<QuickPulseTelemetryModule>()
                                        .Single();
                    quickPulseModule.RegisterTelemetryProcessor(processor);

                    return processor;
                });

                telemetryConfig
                    .DefaultTelemetrySink
                    .TelemetryProcessorChainBuilder
                    .Build();

                // add initializers: https://github.com/Microsoft/ApplicationInsights-aspnetcore/pull/672
                telemetryConfig.TelemetryInitializers.Add(
                        new DomainNameRoleInstanceTelemetryInitializer());
                telemetryConfig.TelemetryInitializers.Add(
                    new HttpDependenciesParsingTelemetryInitializer());
                telemetryConfig.AddApplicationInsightsKubernetesEnricher(
                    applyOptions: null);

                // initialize all modules
                foreach (var module in s.GetServices<ITelemetryModule>())
                {
                    module.Initialize(telemetryConfig);
                }

                // other config: https://github.com/Microsoft/ApplicationInsights-aspnetcore/blob/de1af6235a4cc365d64cbc78db9bdd2d579a37ee/src/Microsoft.ApplicationInsights.AspNetCore/Implementation/TelemetryConfigurationOptionsSetup.cs#L129
                telemetryConfig.ApplicationIdProvider =
                    s.GetRequiredService<IApplicationIdProvider>();

                return telemetryConfig;
            });

            services.AddSingleton<ITelemetryModule>(s =>
            {
                var module = new DependencyTrackingTelemetryModule();

                var excludedDomains = module.ExcludeComponentCorrelationHttpHeadersOnDomains;
                excludedDomains.Add("core.windows.net");
                excludedDomains.Add("core.chinacloudapi.cn");
                excludedDomains.Add("core.cloudapi.de");
                excludedDomains.Add("core.usgovcloudapi.net");

                if (module.EnableLegacyCorrelationHeadersInjection)
                {
                    excludedDomains.Add("localhost");
                    excludedDomains.Add("127.0.0.1");
                }

                var includedActivities = module.IncludeDiagnosticSourceActivities;
                // TODO: in workflow scenario EventHubs activities may not be required.
                includedActivities.Add("Microsoft.Azure.EventHubs");
                includedActivities.Add("Microsoft.Azure.ServiceBus");

                return module;
            });

            services.AddSingleton<
                ITelemetryModule,
                QuickPulseTelemetryModule>();

            services.TryAddSingleton<
                IApplicationIdProvider,
                ApplicationInsightsApplicationIdProvider>();

            services.TryAddSingleton<TelemetryClient>();

            return services;
        }
    }
}