using Fabrikam.DroneDelivery.ApiClient;
using Fabrikam.DroneDelivery.WebSite.Accessors;
using Fabrikam.DroneDelivery.WebSite.Common;
using Fabrikam.DroneDelivery.WebSite.Handlers;
using Fabrikam.DroneDelivery.WebSite.Interfaces;
using Fabrikam.DroneDelivery.WebSite.Manager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace Fabrikam.DroneDelivery.WebSite
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

            // Configure AppInsights
            services.AddApplicationInsightsKubernetesEnricher();
            services.AddApplicationInsightsTelemetry(Configuration);

            // Add health check
            services.AddHealthChecks().AddCheck(
                    HealCheckName,
                    () => HealthCheckResult.Healthy("OK"));

            services.AddControllersWithViews();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            services.AddTransient<IgnoreSSLValidateDelegatingHandler>();

            services.AddSingleton<IConfiguration>(Configuration);

            services.AddHttpClient("delivery", c =>
            {
                c.BaseAddress = new Uri($"{Environment.GetEnvironmentVariable("ApiUrl") ?? Configuration["ApiUrl"]}");
            }).AddHttpMessageHandler<IgnoreSSLValidateDelegatingHandler>();

            services.AddScoped<ITrackingAccessor, TrackingAccessor>();
            services.AddScoped<IDroneManager, DroneManager>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}
