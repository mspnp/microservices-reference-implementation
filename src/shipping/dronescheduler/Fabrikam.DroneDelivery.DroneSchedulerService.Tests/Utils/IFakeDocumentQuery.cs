// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Tests.Utils
{
    public interface IFakeDocumentQuery<T> : 
        IDocumentQuery<T>, 
        IOrderedQueryable<T>
    { }
}
