using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using System.Threading.Tasks;

namespace Fabrikam.Workflow.Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return new HostBuilder()
                .ConfigureAppConfiguration((environment, builder) =>
                {
                    builder.AddEnvironmentVariables();

                    var buildConfig = builder.Build();
                    if (buildConfig["CONFIGURATION_FOLDER"] is var configurationFolder && !string.IsNullOrEmpty(configurationFolder))
                    {
                        builder.AddKeyPerFile(configurationFolder, false);
                    }
                })
                .ConfigureLogging(builder =>
                {
                    var serilogBuilder = new LoggerConfiguration()
                        //.ReadFrom.Configuration(Configuration)
                        //.Enrich.With(new CorrelationLogEventEnricher(httpContextAccessor, Configuration["Logging:CorrelationHeaderKey"]))
                        .WriteTo.Console(new CompactJsonFormatter());

                    builder.AddSerilog(serilogBuilder.CreateLogger(), true);
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<WorkflowService>();
                })
                .UseConsoleLifetime();
        }
    }
}
