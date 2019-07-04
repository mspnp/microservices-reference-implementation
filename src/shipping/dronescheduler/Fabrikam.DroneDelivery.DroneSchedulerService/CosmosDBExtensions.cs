// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;
using Fabrikam.DroneDelivery.DroneSchedulerService.Services;

namespace Fabrikam.DroneDelivery.DroneSchedulerService
{
    public static class CosmosDBExtensions
    {
        public static IServiceCollection AddCosmosRepository<T>(
                 this IServiceCollection services,
                 IConfiguration config)
            where T : BaseDocument
        {
            services.AddSingleton<IDocumentClient>(s =>
                {
                    var options = s.GetRequiredService<IOptions<CosmosDBConnectionOptions>>();

                    return new DocumentClient(
                        new Uri(options.Value.CosmosDBEndpoint),
                        options.Value.CosmosDBKey,
                        connectionPolicy: new ConnectionPolicy { ConnectionMode = options.Value.CosmosDBConnectionMode });
                });
            services.ConfigureOptions<ConfigureCosmosDBRepositoryOptions<T>>();

            if (services.Any(sr => sr.ServiceType == typeof(CosmosDBConnectionOptions)) == false)
            {
                services.Configure<CosmosDBConnectionOptions>(config);
            }

            services.AddSingleton<ICosmosRepository<T>, CosmosRepository<T>>();
            services.AddSingleton<ICosmosDBRepositoryMetricsTracker<T>, CosmosDBRepositoryMetricsTracker<T>>();

            return services;
        }
    }
}