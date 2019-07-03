// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public class InvoicingRepository : IInvoicingRepository
    {
        private ICosmosRepository<InternalDroneUtilization> _repository;

        public InvoicingRepository(
                ICosmosRepository<InternalDroneUtilization> repository)
            => this._repository = repository;

        public async Task<IEnumerable<InternalDroneUtilization>> GetItemsAsync(
                Expression<Func<InternalDroneUtilization, bool>> predicate,
                string partitionKey)
        {
            return await _repository
                .GetItemsAsync(
                        predicate,
                        partitionKey);
        }

        public async Task<Tuple<double, double>> GetAggreatedInvoincingDataAsync(
                string ownerId,
                int year,
                int month)
        {
            var results = await this.GetItemsAsync(
                    d => d.Year == year && d.Month == month,
                    ownerId);

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