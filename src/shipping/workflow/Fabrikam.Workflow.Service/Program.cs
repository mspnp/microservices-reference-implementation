using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using System.IO;
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
                        //.Enrich.With(new CorrelationLogEventEnricher(httpContextAccessor, Configuration["Logging:CorrelationHeaderKey"]))
                        .WriteTo.Console(new CompactJsonFormatter());

                    builder.AddSerilog(serilogBuilder.CreateLogger(), true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions();
                    services.Configure<WorkflowServiceOptions>(context.Configuration);
                    services.AddHostedService<WorkflowService>();
                })
                .UseConsoleLifetime();
        }
    }
}
