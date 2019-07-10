// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Documents.Client;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public class CosmosDBRepositoryMetricsTracker<T>
        : ICosmosDBRepositoryMetricsTracker<T>
        where T : BaseDocument
    {
        private const string RequestUnitsMetricId = "CosmosDb-RequestUnits";
        private const string CollectionDimensionName = "Collection";
        private const string DocumentDimensionName = "Document";
        private const string PartitionKeyDimensionName = "PartitionKey";
        private const string DocumentCountDimensionName = "DocumentCount";
        private const string RequestChargeKey = "RequestCharge";

        private readonly TelemetryClient _telemetryClient;
        private readonly Metric _metric;

        public CosmosDBRepositoryMetricsTracker(TelemetryClient telemetryClient)
        {
            this._telemetryClient = telemetryClient;
            this._metric =
                telemetryClient.GetMetric(
                    RequestUnitsMetricId,
                    CollectionDimensionName,
                    DocumentDimensionName,
                    PartitionKeyDimensionName,
                    DocumentCountDimensionName);
        }

        public ICosmosDBRepositoryQueryMetricsTracker<T> GetQueryMetricsTracker(string collection, string partitionKey)
        {
            return new QueryMetricsTracker(this._telemetryClient, this._metric, collection, partitionKey);
        }

        public void TrackResponseMetrics(FeedResponse<T> response, string collection, string partitionKey)
        {
        }

        private sealed class QueryMetricsTracker : ICosmosDBRepositoryQueryMetricsTracker<T>
        {
            private readonly TelemetryClient _telemetryClient;
            private readonly Metric _metric;
            private readonly string _collection;
            private readonly string _partitionKey;

            private double _totalCharge;
            private long _totalDocumentCount;


            public QueryMetricsTracker(TelemetryClient telemetryClient, Metric metric, string collection, string partitionKey)
            {
                this._telemetryClient = telemetryClient;
                this._metric = metric;
                this._collection = collection;
                this._partitionKey = partitionKey;
            }

            public void Dispose()
            {
                this._telemetryClient.TrackTrace(
                    "Completed document query",
                    SeverityLevel.Information,
                    new Dictionary<string, string>
                    {
                        [RequestChargeKey] = this._totalCharge.ToString(CultureInfo.InvariantCulture),
                        [CollectionDimensionName] = this._collection,
                        [DocumentDimensionName] = typeof(T).Name,
                        [PartitionKeyDimensionName] = this._partitionKey,
                        [DocumentCountDimensionName] = this._totalDocumentCount.ToString(CultureInfo.InvariantCulture)
                    });
            }

            public void TrackResponseMetrics(FeedResponse<T> response)
            {
                var responseCount = response.Count.ToString(CultureInfo.InvariantCulture);

                this._metric.TrackValue(
                    response.RequestCharge,
                    this._collection,
                    typeof(T).Name,
                    this._partitionKey ?? "<none>",
                    responseCount);

                this._telemetryClient.TrackTrace(
                    "Partial document query",
                    SeverityLevel.Information,
                    new Dictionary<string, string>
                    {
                        [RequestChargeKey] = response.RequestCharge.ToString(CultureInfo.InvariantCulture),
                        [CollectionDimensionName] = this._collection,
                        [DocumentDimensionName] = typeof(T).Name,
                        [PartitionKeyDimensionName] = this._partitionKey,
                        [DocumentCountDimensionName] = responseCount
                    });

                this._totalCharge += response.RequestCharge;
                this._totalDocumentCount += response.Count;
            }
        }
    }
}
