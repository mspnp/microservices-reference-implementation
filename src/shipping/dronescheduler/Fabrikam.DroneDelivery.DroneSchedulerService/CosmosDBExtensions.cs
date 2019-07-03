// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;
using Fabrikam.DroneDelivery.DroneSchedulerService.Services;
using Microsoft.Azure.Documents;

namespace Fabrikam.DroneDelivery.DroneSchedulerService
{
    public static class CosmosDBExtensions
    {
        public static IServiceCollection AddCosmosRepository<T>(
                 this IServiceCollection services,
                 IConfiguration config)
            where T : BaseDocument
        {
            var endpoint = config["CosmosDB-Endpoint"];
            var key = config["CosmosDB-Key"];

            services.AddSingleton<IDocumentClient>(s => new DocumentClient(new Uri(endpoint), key));
            services.ConfigureOptions<ConfigureCosmosDBRepositoryOptions<T>>();

            services.AddSingleton<ICosmosRepository<T>, CosmosRepository<T>>();

            return services;
        }
    }
}