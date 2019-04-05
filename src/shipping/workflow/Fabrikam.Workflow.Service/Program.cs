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
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace Fabrikam.Workflow.Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Fabrikan Workflow Service is starting.");

            await host.RunAsync();
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
                    builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                    builder.AddApplicationInsights();

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
