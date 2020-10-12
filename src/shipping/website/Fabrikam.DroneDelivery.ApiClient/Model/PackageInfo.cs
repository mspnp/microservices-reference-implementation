using System;
using System.Collections.Generic;
using System.Text;

namespace Fabrikam.DroneDelivery.ApiClient.Model
{    
    public class PackageInfo
    {
        public string packageId { get; set; }
        public string size { get; set; }
        public string tag { get; set; }
        public int weight { get; set; }
    }
}
