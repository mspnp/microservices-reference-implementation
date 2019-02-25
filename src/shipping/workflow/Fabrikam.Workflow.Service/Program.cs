// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace Fabrikam.Workflow.Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            TelemetryClient telemetryClient = null;
            try
            {
                telemetryClient = host.Services.GetService<TelemetryClient>();

                telemetryClient.TrackTrace("Fabrikan Workflow Service is starting.");

                await host.RunAsync();
            }
            finally
            {
                // before exit, flush the remaining data
                telemetryClient?.Flush();

                // flush is not blocking so wait a bit
                Task.Delay(5000).Wait();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return new HostBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();

                    var buildConfig = builder.Build();
                    if (buildConfig["CONFIGURATION_FOLDER"] is var configurationFolder && !string.IsNullOrEmpty(configurationFolder))
                    {
                        builder.AddKeyPerFile(Path.Combine(context.HostingEnvironment.ContentRootPath, configurationFolder), false);
                    }
                })
                .ConfigureLogging((context, builder) =>
                {
                    var serilogBuilder = new LoggerConfiguration()
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.Console(new CompactJsonFormatter());

                    builder.AddSerilog(serilogBuilder.CreateLogger(), true);
                })
                .ConfigureServices(ServiceStartup.ConfigureServices)
                .UseConsoleLifetime();
        }
    }
}