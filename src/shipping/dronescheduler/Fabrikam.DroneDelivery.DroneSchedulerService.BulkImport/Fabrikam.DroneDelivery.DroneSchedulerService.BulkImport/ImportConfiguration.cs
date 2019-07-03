// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.BulkImport
{
    internal class ImportConfiguration
    {
        public string EndpointUrl { get; set; }

        public string AuthorizationKey { get; set; }

        public string DatabaseName { get; set; }

        public string CollectionName { get; set; }

        public string CollectionPartitionKey { get; set; }

        public int CollectionThroughput { get; set; }

        public int NumberOfBatches { get; set; }

        public long NumberOfDocuments { get; set; }

        public int NumberDocumentsPerPartitionExpFactor { get; set; }

        public string DocumentTypeName { get; set; }

        public bool FlattenPartitionKey { get; set; }

        public long NumberOfDocumentsPerBatch()
        {
            return (long)Math.Floor(((double)NumberOfDocuments) / NumberOfBatches);
        }
    }
}