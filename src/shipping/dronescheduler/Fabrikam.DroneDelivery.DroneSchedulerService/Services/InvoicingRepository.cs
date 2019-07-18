// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.FeatureManagement;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public class InvoicingRepository : IInvoicingRepository
    {
        private const string UsePartitionKeyFeatureName = "UsePartitionKey";
        private readonly ICosmosRepository<InternalDroneUtilization> _repository;
        private readonly IFeatureManager _featureManager;

        public InvoicingRepository(
                ICosmosRepository<InternalDroneUtilization> repository,
                IFeatureManager featureManager)
        {
            this._repository = repository;
            this._featureManager = featureManager;
        }

        public async Task<Tuple<double, double>> GetAggreatedInvoincingDataAsync(
                string ownerId,
                int year,
                int month)
        {
            (QueryDefinition query, string partitionKey) =
                this._featureManager.IsEnabled(UsePartitionKeyFeatureName)
                    ? (new QueryDefinition("SELECT VALUE root FROM root WHERE root.year = @year AND root.month = @month")
                            .WithParameter("@year", year)
                            .WithParameter("@month", month), 
                        ownerId)
                    : (new QueryDefinition("SELECT VALUE root FROM root WHERE root.year = @year AND root.month = @month AND root.ownerId = @ownerId")
                            .WithParameter("@year", year)
                            .WithParameter("@month", month)
                            .WithParameter("@ownerId", ownerId), 
                        default(string));

            var results = await _repository.GetItemsAsync(query, partitionKey);

            var result = results
                .Aggregate(
                    new
                    {
                        TraveledMiles = 0d,
                        AssignedHours = 0d
                    },
                    (c, n) => new
                    {
                        TraveledMiles = c.TraveledMiles + n.TraveledMiles,
                        AssignedHours = c.AssignedHours + n.AssignedHours
                    });

            return Tuple.Create<double, double>(
                    result.TraveledMiles,
                    result.AssignedHours);
        }
    }
}