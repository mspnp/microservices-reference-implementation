// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Cosmos;
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
        private const string CosmosDbPropertyPrefix = "CosmosDb.";
        private const string CollectionPropertyKey = CosmosDbPropertyPrefix + CollectionDimensionName;
        private const string DocumentPropertyKey = CosmosDbPropertyPrefix + DocumentDimensionName;
        private const string PartitionKeyPropertyKey = CosmosDbPropertyPrefix + PartitionKeyDimensionName;
        private const string DocumentCountPropertyKey = CosmosDbPropertyPrefix + DocumentCountDimensionName;
        private const string RequestChargePropertyKey = CosmosDbPropertyPrefix + "RequestCharge";
        private const string MaxParallelismPropertyKey = CosmosDbPropertyPrefix + "MaxParallelism";
        private const string MaxConnectionsPropertyKey = CosmosDbPropertyPrefix + "MaxConnections";
        private const string ConnectionModePropertyKey = CosmosDbPropertyPrefix + "ConnectionMode";
        private const string MaxBufferedItemCountPropertyKey = CosmosDbPropertyPrefix + "MaxBufferedItemCount";

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

        public ICosmosDBRepositoryQueryMetricsTracker<T> GetQueryMetricsTracker(
            string collection,
            string partitionKey,
            int maxParallelism,
            int maxConnections,
            ConnectionMode connectionMode,
            int maxBufferedItemCount)
        {
            return new QueryMetricsTracker(
                this._telemetryClient,
                this._metric,
                collection,
                partitionKey,
                maxParallelism,
                maxConnections,
                connectionMode,
                maxBufferedItemCount);
        }

        private sealed class QueryMetricsTracker : ICosmosDBRepositoryQueryMetricsTracker<T>
        {
            private readonly TelemetryClient _telemetryClient;
            private readonly Metric _metric;
            private readonly string _collection;
            private readonly string _partitionKey;
            private readonly string _maxParallelism;
            private readonly string _maxConnections;
            private readonly string _connectionMode;
            private readonly string _maxBufferedItemCount;
            private double _totalCharge;
            private long _totalDocumentCount;


            public QueryMetricsTracker(
                TelemetryClient telemetryClient,
                Metric metric,
                string collection,
                string partitionKey,
                int maxParallelism,
                int maxConnections,
                ConnectionMode connectionMode,
                int maxBufferedItemCount)
            {
                this._telemetryClient = telemetryClient;
                this._metric = metric;
                this._collection = collection;
                this._partitionKey = partitionKey;
                this._maxParallelism = maxParallelism.ToString(CultureInfo.InvariantCulture);
                this._maxConnections = maxConnections.ToString(CultureInfo.InvariantCulture);
                this._connectionMode = connectionMode.ToString();
                this._maxBufferedItemCount = maxBufferedItemCount.ToString(CultureInfo.InvariantCulture);
            }

            public void Dispose()
            {
                TraceMetrics(
                    "Completed document query",
                    this._totalCharge.ToString(CultureInfo.InvariantCulture),
                    this._totalDocumentCount.ToString(CultureInfo.InvariantCulture));
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

                TraceMetrics(
                    "Partial document query",
                    response.RequestCharge.ToString(CultureInfo.InvariantCulture),
                    responseCount);

                this._totalCharge += response.RequestCharge;
                this._totalDocumentCount += response.Count;
            }

            private void TraceMetrics(string message, string requestCharge, string documentCount)
            {
                this._telemetryClient.TrackTrace(
                    message,
                    SeverityLevel.Information,
                    new Dictionary<string, string>
                    {
                        [RequestChargePropertyKey] = requestCharge,
                        [CollectionPropertyKey] = this._collection,
                        [DocumentPropertyKey] = typeof(T).Name,
                        [PartitionKeyPropertyKey] = this._partitionKey,
                        [DocumentCountPropertyKey] = documentCount,
                        [MaxParallelismPropertyKey] = this._maxParallelism,
                        [MaxConnectionsPropertyKey] = this._maxConnections,
                        [ConnectionModePropertyKey] = this._connectionMode,
                        [MaxBufferedItemCountPropertyKey] = this._maxBufferedItemCount,
                    });
            }
        }
    }
}