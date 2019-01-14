// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Fabrikam.Workflow.Service.Models;

namespace Fabrikam.Workflow.Service.RequestProcessing
{
    public interface IRequestProcessor
    {
        Task<bool> ProcessDeliveryRequestAsync(Delivery deliveryRequest, IReadOnlyDictionary<string, object> properties);
    }
}
