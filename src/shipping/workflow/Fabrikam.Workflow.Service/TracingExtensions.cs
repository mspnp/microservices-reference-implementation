// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
        public static IServiceCollection AddApplicationInsightsTelemetry(
          this IServiceCollection services,
          IConfiguration configuration)
        {
            // add initializers
            services.AddSingleton<
                ITelemetryInitializer,
                DomainNameRoleInstanceTelemetryInitializer>();
            services.AddSingleton<
                ITelemetryInitializer,
                HttpDependenciesParsingTelemetryInitializer>();

            // add modules
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
                includedActivities.Add("Microsoft.Azure.ServiceBus");

                return module;
            });

            services.AddSingleton<
                ITelemetryModule,
                QuickPulseTelemetryModule>();

            // add others
            services.TryAddSingleton<
                IApplicationIdProvider,
                ApplicationInsightsApplicationIdProvider>();

            services.TryAddSingleton<
                ITelemetryChannel,
                ServerTelemetryChannel>();

            services.TryAddSingleton<TelemetryClient>();

            services.AddSingleton(provider =>
                provider.GetService<IOptions<TelemetryConfiguration>>().Value);

            services.AddOptions();
            services.AddSingleton<IOptions<TelemetryConfiguration>, TelemetryConfigurationOptions>();
            services.AddSingleton<IConfigureOptions<TelemetryConfiguration>, TelemetryConfigurationOptionsSetup>();

            return services;
        }

        public static IServiceCollection AddApplicationInsightsKubernetesEnricher (this IServiceCollection services)
        {
            services.Configure<TelemetryConfiguration>(
                (config) =>
                    config.AddApplicationInsightsKubernetesEnricher(
                            applyOptions: null)
            );

            return services;
        }

        private class TelemetryConfigurationOptionsSetup : IConfigureOptions<TelemetryConfiguration>
        {
            private const string CustomKeyVaultAppInsightsIKey = "ApplicationInsights-InstrumentationKey";
            private const string AppInsightsDeveloperMode = "ApplicationInsights:DeveloperMode";

            private readonly IConfiguration _configuration;
            private readonly IServiceProvider _serviceProvider;
            private readonly IEnumerable<ITelemetryInitializer> _initializers;
            private readonly IEnumerable<ITelemetryModule> _modules;
            private readonly ITelemetryChannel _telemetryChannel;

            public TelemetryConfigurationOptionsSetup(
                IServiceProvider serviceProvider,
                IEnumerable<ITelemetryInitializer> initializers,
                IEnumerable<ITelemetryModule> modules,
                ITelemetryChannel telemetryChannel,
                IConfiguration configuration)
            {
                this._serviceProvider = serviceProvider;
                this._initializers = initializers;
                this._modules = modules;
                this._telemetryChannel = telemetryChannel;
                this._configuration = configuration;
            }

            public void Configure(TelemetryConfiguration telemetryConfig)
            {
                // flex volume
                var instrumentationKey = _configuration[CustomKeyVaultAppInsightsIKey];

                if (!string.IsNullOrWhiteSpace(instrumentationKey))
                {
                    telemetryConfig.InstrumentationKey = instrumentationKey;
                }

                // Fallback to default channel (InMemoryChannel) created by base sdk if no channel is found in DI
                telemetryConfig.TelemetryChannel =
                    this._telemetryChannel
                    ?? telemetryConfig.TelemetryChannel;

                if(bool.TryParse(_configuration[AppInsightsDeveloperMode], out bool developerMode))
                    this._telemetryChannel.DeveloperMode = developerMode;

                (telemetryConfig.TelemetryChannel as ITelemetryModule)
                    ?.Initialize(telemetryConfig);

                // use processors
                telemetryConfig
                    .DefaultTelemetrySink
                    .TelemetryProcessorChainBuilder
                    .Use((next) =>
                {
                    var processor = new QuickPulseTelemetryProcessor(next);

                    var quickPulseModule = _serviceProvider
                                            .GetServices<ITelemetryModule>()
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
                foreach (var initializers in _initializers)
                {
                    telemetryConfig.TelemetryInitializers.Add(initializers);
                }

                // initialize all modules
                foreach (var module in _modules)
                {
                    module.Initialize(telemetryConfig);
                }

                // other config: https://github.com/Microsoft/ApplicationInsights-aspnetcore/blob/de1af6235a4cc365d64cbc78db9bdd2d579a37ee/src/Microsoft.ApplicationInsights.AspNetCore/Implementation/TelemetryConfigurationOptionsSetup.cs#L129
                telemetryConfig.ApplicationIdProvider =
                    _serviceProvider.GetRequiredService<IApplicationIdProvider>();
            }
        }

        private class TelemetryConfigurationOptions : IOptions<TelemetryConfiguration>
        {
            public TelemetryConfigurationOptions(IEnumerable<IConfigureOptions<TelemetryConfiguration>> configureOptions)
            {
                this.Value = TelemetryConfiguration.CreateDefault();

                var configureOptionsArray = configureOptions.ToArray();
                foreach (var c in configureOptionsArray)
                {
                    c.Configure(this.Value);
                }
            }

            public TelemetryConfiguration Value { get; }
        }
    }
}
