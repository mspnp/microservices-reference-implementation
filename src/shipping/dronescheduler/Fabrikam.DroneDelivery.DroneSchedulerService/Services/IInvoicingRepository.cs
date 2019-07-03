// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public interface IInvoicingRepository
    {
        Task<IEnumerable<InternalDroneUtilization>> GetItemsAsync(
                Expression<Func<InternalDroneUtilization, bool>> predicate,
                string partitionKey);

        Task<Tuple<double,double>> GetAggreatedInvoincingDataAsync(
                string ownerId,
                int year,
                int month);
    }
}
