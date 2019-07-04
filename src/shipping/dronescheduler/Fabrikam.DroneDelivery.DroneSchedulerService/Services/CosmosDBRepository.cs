// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public class CosmosRepository<T> : ICosmosRepository<T>
        where T : BaseDocument
    {
        private readonly IDocumentClient _client;
        private readonly CosmosDBRepositoryOptions<T> _options;
        private readonly ILogger<CosmosRepository<T>> _logger;
        private readonly ICosmosDBRepositoryMetricsTracker<T> _metricsTracker;

        public CosmosRepository(
                IDocumentClient client,
                IOptions<CosmosDBRepositoryOptions<T>> options,
                ILogger<CosmosRepository<T>> logger,
                ICosmosDBRepositoryMetricsTracker<T> metricsTracker)
        {
            this._client = client;
            this._options = options.Value;
            this._logger = logger;
            this._metricsTracker = metricsTracker;
        }

        public async Task<IEnumerable<T>> GetItemsAsync(
                Expression<Func<T, bool>> predicate,
                string partitionKey)
        {
            using (_logger.BeginScope(nameof(GetItemsAsync)))
            {
                _logger.LogInformation(
                        "partitionKey: {PartitionKey}",
                        partitionKey);

                IDocumentQuery<T> query =
                    _client.CreateDocumentQuery<T>(
                        this._options.CollectionUri,
                        new FeedOptions
                        {
                            MaxItemCount = this._options.MaxParallelism,
                            PartitionKey = partitionKey != null ? new PartitionKey(partitionKey) : null,
                            EnableCrossPartitionQuery = partitionKey == null
                        })
                    .Where(predicate)
                    .Where(d => d.DocumentType == typeof(T).Name)
                .AsDocumentQuery();

                var results = new List<T>();

                _logger.LogInformation("Start: reading results from query");
                while (query.HasMoreResults)
                {
                    var feed = await query.ExecuteNextAsync<T>();
                    this._metricsTracker.TrackResponseMetrics(feed, this._options.CollectionUri.ToString(), partitionKey);
                    results.AddRange(feed);
                }

                _logger.LogInformation("End: reading results from query");

                return results;
            }
        }
    }
}
