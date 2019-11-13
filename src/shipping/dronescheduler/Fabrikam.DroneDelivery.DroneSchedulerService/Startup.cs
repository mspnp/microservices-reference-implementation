// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;
using Fabrikam.DroneDelivery.DroneSchedulerService.Services;
using Serilog;
using Serilog.Formatting.Compact;
using Swashbuckle.AspNetCore.Swagger;

namespace Fabrikam.DroneDelivery.DroneSchedulerService
{
    public class Startup
    {
        private const string HealCheckName = "ReadinessLiveness";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddFeatureManagement();

            // Configure AppInsights
            services.AddApplicationInsightsKubernetesEnricher();
            services.AddApplicationInsightsTelemetry(Configuration);

            // Add framework services.
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Add health check
            services.AddHealthChecks().AddCheck(
                    HealCheckName,
                    () => HealthCheckResult.Healthy("OK"));

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Mock DroneScheduler API", Version = "v1" });
            });

            services.AddSingleton<IInvoicingRepository, InvoicingRepository>();
            services
                .AddCosmosRepository<
                InternalDroneUtilization>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor)
        {
            Log.Logger = new LoggerConfiguration()
              .WriteTo.Console(new CompactJsonFormatter())
              .ReadFrom.Configuration(Configuration)
              .CreateLogger();

            // Map health checks
            app.UseHealthChecks("/healthz");

            app.UseMvc();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mock DroneScheduler API V1");
            });
        }
    }
}
