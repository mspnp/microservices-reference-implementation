// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public class CosmosRepository<T> : ICosmosRepository<T>
        where T : BaseDocument
    {
        private readonly CosmosClient _client;
        private readonly CosmosDBRepositoryOptions<T> _options;
        private readonly ILogger<CosmosRepository<T>> _logger;
        private readonly ICosmosDBRepositoryMetricsTracker<T> _metricsTracker;
        private readonly string _collectionIdentifier;

        public CosmosRepository(
                CosmosClient client,
                IOptions<CosmosDBRepositoryOptions<T>> options,
                ILogger<CosmosRepository<T>> logger,
                ICosmosDBRepositoryMetricsTracker<T> metricsTracker)
        {
            this._client = client;
            this._options = options.Value;
            this._logger = logger;
            this._metricsTracker = metricsTracker;
            this._collectionIdentifier = $"{options.Value.DatabaseId}.{options.Value.CollectionId}";
        }

        public async Task<IEnumerable<T>> GetItemsAsync(
                QueryDefinition query,
                string partitionKey)
        {
            var results = new List<T>();

            using (_logger.BeginScope(nameof(GetItemsAsync)))
            {
                _logger.LogInformation(
                        "partitionKey: {PartitionKey}",
                        partitionKey);

                using (var queryMetricsTracker =
                    this._metricsTracker.GetQueryMetricsTracker(
                        this._collectionIdentifier,
                        partitionKey,
                        this._options.MaxParallelism,
                        this._client.ClientOptions.GatewayModeMaxConnectionLimit,
                        this._client.ClientOptions.ConnectionMode,
                        this._options.MaxBufferedItemCount))
                {
                    FeedIterator<T> iterator =
                        this._options.Container.GetItemQueryIterator<T>(
                            query,
                            requestOptions: new QueryRequestOptions
                            {
                                MaxConcurrency = this._options.MaxParallelism,
                                PartitionKey = partitionKey != null ? new PartitionKey(partitionKey) : new PartitionKey?(),
                                MaxBufferedItemCount = this._options.MaxBufferedItemCount
                            });

                    _logger.LogInformation("Start: reading results from query");
                    while (iterator.HasMoreResults)
                    {
                        var feed = await iterator.ReadNextAsync();
                        queryMetricsTracker.TrackResponseMetrics(feed);
                        results.AddRange(feed);
                    }
                }

                _logger.LogInformation("End: reading results from query");

                return results;
            }
        }
    }
}
