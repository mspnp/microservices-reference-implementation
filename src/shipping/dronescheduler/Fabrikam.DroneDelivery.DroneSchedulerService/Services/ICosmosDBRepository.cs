// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Services
{
    public interface ICosmosRepository<T>
    {
        Task<IEnumerable<T>> GetItemsAsync(
                QueryDefinition query,
                string partitionKey);
    }
}
