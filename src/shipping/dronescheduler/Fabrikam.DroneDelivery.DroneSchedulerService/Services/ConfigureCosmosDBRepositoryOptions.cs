// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public class ConfigureCosmosDBRepositoryOptions<T> : IConfigureOptions<CosmosDBRepositoryOptions<T>>
        where T : BaseDocument
    {
        private readonly IConfiguration _config;
        private readonly IDocumentClient _client;

        public ConfigureCosmosDBRepositoryOptions(
            IConfiguration config,
            IDocumentClient client)
        {
            _config = config;
            _client = client;
        }

        public void Configure(CosmosDBRepositoryOptions<T> options)
        {
            options.DatabaseId = _config["COSMOSDB_DATABASEID"];
            options.CollectionId = _config["COSMOSDB_COLLECTIONID"];

            Database db = _client
                .CreateDatabaseQuery()
                .Where(d => d.Id == options.DatabaseId)
                .AsEnumerable()
                .FirstOrDefault();

            if (db != null)
            {
                DocumentCollection col = _client
                    .CreateDocumentCollectionQuery(db.SelfLink)
                    .Where(d => d.Id == options.CollectionId)
                    .AsEnumerable()
                    .FirstOrDefault();

                if (Uri.TryCreate(col?.SelfLink, UriKind.Relative, out Uri uri))
                {
                    options.CollectionUri = uri;
                }
            }

            if (string.IsNullOrEmpty(_config["CosmosDBMaxParallelism"]) == false)
            {
                options.MaxParallelism = int.Parse(_config["CosmosDBMaxParallelism"], CultureInfo.InvariantCulture);
            }
        }
    }
}