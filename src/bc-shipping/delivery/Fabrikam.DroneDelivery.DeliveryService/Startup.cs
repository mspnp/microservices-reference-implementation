// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using Fabrikam.DroneDelivery.DeliveryService.Models;
using Fabrikam.DroneDelivery.DeliveryService.Services;
using Fabrikam.DroneDelivery.DeliveryService.Middlewares.Builder;
using Serilog;
using Serilog.Formatting.Compact;
using Fabrikam.DroneDelivery.Common;

namespace Fabrikam.DroneDelivery.DeliveryService
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            // Add framework services.
            services.AddMvc();

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Fabrikam DroneDelivery DeliveryService API", Version = "v1" });
            });
            
            services.AddSingleton<IDeliveryRepository, DeliveryRepository>();
            services.AddSingleton<INotifyMeRequestRepository, NotifyMeRequestRepository>();
            services.AddSingleton<INotificationService, NoOpNotificationService>();
            services.AddSingleton<IDeliveryHistoryService, DeliveryHistoryService>();
            services.AddSingleton<IDeliveryTrackingEventRepository, DeliveryTrackingRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor)
        {
            Log.Logger = new LoggerConfiguration()
              .WriteTo.Console(new CompactJsonFormatter())
              .ReadFrom.Configuration(Configuration)
              .Enrich.With(new CorrelationLogEventEnricher(httpContextAccessor, Configuration["Logging:CorrelationHeaderKey"]))
              .CreateLogger();

            // Important: it has to be first: enable global logger
            app.UseGlobalLoggerHandler();

            // Important: it has to be second: Enable global exception, error handling
            app.UseGlobalExceptionHandler();

            // TODO: Add middleware AuthZ here

            app.UseMvc();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fabrikam DroneDelivery DeliveryService API V1");
            });

            //TODO look into creating a factory of DocDBRepos/RedisCache/EventHubMessenger
            DocumentDBRepository<InternalNotifyMeRequest>.Configure(Configuration["DOCDB_ENDPOINT"], Configuration["DOCDB_KEY"], Configuration["DOCDB_DATABASEID"], Configuration["DOCDB_COLLECTIONID"], loggerFactory);
            RedisCache<InternalDelivery>.Configure(Constants.RedisCacheDBId_Delivery, Configuration["REDIS_CONNSTR"], loggerFactory);
            EventHubSender<DeliveryHistory>.Configure(Configuration["EH_CONNSTR"], Configuration["EH_ENTITYPATH"]);
        }
    }
}
