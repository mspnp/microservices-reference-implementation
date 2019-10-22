// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Globalization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public class ConfigureCosmosDBRepositoryOptions<T> : IConfigureOptions<CosmosDBRepositoryOptions<T>>
        where T : BaseDocument
    {
        private readonly IConfiguration _config;
        private readonly CosmosClient _client;

        public ConfigureCosmosDBRepositoryOptions(
            IConfiguration config,
            CosmosClient client)
        {
            _config = config;
            _client = client;
        }

        public void Configure(CosmosDBRepositoryOptions<T> options)
        {
            options.DatabaseId = _config["COSMOSDB_DATABASEID"];
            options.CollectionId = _config["COSMOSDB_COLLECTIONID"];

            options.Container = _client
                .GetContainer(options.DatabaseId, options.CollectionId);
            
            if (string.IsNullOrEmpty(_config["CosmosDBMaxParallelism"]) == false)
            {
                options.MaxParallelism = int.Parse(_config["CosmosDBMaxParallelism"], CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrEmpty(_config["CosmosDBMaxBufferedItemCount"]) == false)
            {
                options.MaxBufferedItemCount = int.Parse(_config["CosmosDBMaxBufferedItemCount"], CultureInfo.InvariantCulture);
            }
        }
    }
}