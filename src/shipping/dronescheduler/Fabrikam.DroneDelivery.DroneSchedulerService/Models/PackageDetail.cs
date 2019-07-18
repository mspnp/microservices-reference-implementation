// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Models
{
    public class PackageDetail
    {
        public string Id { get; set; }
        public PackageSize Size { get; set; }
    }
}
