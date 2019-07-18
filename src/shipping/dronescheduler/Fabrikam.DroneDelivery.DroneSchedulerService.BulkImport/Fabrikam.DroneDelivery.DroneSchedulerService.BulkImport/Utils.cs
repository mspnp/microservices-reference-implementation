// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.BulkImport
{
    internal static class Utils
    {
        internal static String GenerateSyntheticDoc(
                string id,
                int batchNumber,
                int docNumberCurrentBatch,
                string docTypeName,
                string partitionKeyProperty,
                bool flattenPartitionKey)
        {
            const string deliveryPrefix = "d000";
            const string ownerPrefix = "o000";
            const double MinMiles = 30d;
            const double MaxMiles = 500d;
            const double MinHours = 1d;
            const double MaxHours = 30d;

            string deliveryId = string.Concat(deliveryPrefix, id);
            string ownerId = string.Concat(ownerPrefix, docNumberCurrentBatch);

            int year = DateTime.UtcNow.Year + (batchNumber / 12);
            int month = batchNumber % 12 + 1;

            var random = new Random();

            double traveledMiles = MinMiles
                + random.NextDouble()
                * (MaxMiles - MinMiles);
            double assignedHours = MinHours
                + random.NextDouble()
                * (MaxHours - MinHours);

            string partitonKeyValue = flattenPartitionKey
                                      ? deliveryId
                                      : ownerId;

            return "{\n" +
                "    \"id\": \"" + deliveryId + "\",\n" +
                "    \"" + partitionKeyProperty + "\": \"" + partitonKeyValue + "\",\n" +
                "    \"type\": \"" + docTypeName + "\",\n" +
                "    \"ownerId\": \"" + ownerId + "\",\n" +
                "    \"travelledMiles\": " + traveledMiles.ToString() + ",\n" +
                "    \"assignedHours\": " + assignedHours.ToString() + ",\n" +
                "    \"year\": " + year.ToString() + ",\n" +
                "    \"month\": " + month.ToString() + "\n" +
                "}";
        }
    }
}
