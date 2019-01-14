// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.Workflow.Service.Models
{
    public class PackageGen
    {
        public string Id { get; set; }
        public ContainerSize Size { get; set; }
        public string Tag { get; set; }
        public double Weight { get; set; }
    }
}
