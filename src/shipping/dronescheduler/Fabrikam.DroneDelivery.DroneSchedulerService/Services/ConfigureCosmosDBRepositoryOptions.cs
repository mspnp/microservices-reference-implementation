// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
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
            Database db = _client
                .CreateDatabaseQuery()
                .Where(d => d.Id == _config["COSMOSDB_DATABASEID"])
                .AsEnumerable()
                .FirstOrDefault();

            if (db != null)
            {
                DocumentCollection col = _client
                    .CreateDocumentCollectionQuery(db.SelfLink)
                    .Where(d => d.Id == _config["COSMOSDB_COLLECTIONID"])
                    .AsEnumerable()
                    .FirstOrDefault();

                if (Uri.TryCreate(col?.SelfLink, UriKind.Relative, out Uri uri))
                {
                    options.CollectionUri = uri;
                }
            }
        }
    }
}