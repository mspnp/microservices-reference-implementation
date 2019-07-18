// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public interface IInvoicingRepository
    {
        Task<Tuple<double, double>> GetAggreatedInvoincingDataAsync(
                string ownerId,
                int year,
                int month);
    }
}
