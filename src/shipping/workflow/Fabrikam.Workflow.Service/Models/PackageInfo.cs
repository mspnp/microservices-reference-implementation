// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Fabrikam.Workflow.Service.Models
{
    public class PackageInfo
    {
        [JsonProperty("packageId")]
        public string PackageId { get; set; }
        [JsonProperty("size")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ContainerSize Size { get; set; }
        [JsonProperty("weight")]
        public double Weight { get; set; }
        [JsonProperty("tag")]
        public string Tag { get; set; }
    }
}
