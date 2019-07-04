// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Globalization;
using Microsoft.ApplicationInsights;
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

        private readonly Metric _metric;

        public CosmosDBRepositoryMetricsTracker(TelemetryClient telemetryClient)
        {
            this._metric =
                telemetryClient.GetMetric(
                    RequestUnitsMetricId,
                    CollectionDimensionName,
                    DocumentDimensionName,
                    PartitionKeyDimensionName,
                    DocumentCountDimensionName);
        }

        public void TrackResponseMetrics(FeedResponse<T> response, string collection, string partitionKey)
        {
            this._metric.TrackValue(
                response.RequestCharge,
                collection,
                typeof(T).Name,
                partitionKey ?? "<none>",
                response.Count.ToString(CultureInfo.InvariantCulture));
        }
    }
}
