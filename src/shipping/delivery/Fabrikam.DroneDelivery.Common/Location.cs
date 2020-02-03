// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.DroneDelivery.Common
{
    public class Location
    {
        public Location() {}

        public Location(double altitude, double latitude, double longitude)
        {
            Altitude = altitude;
            Latitude = latitude;
            Longitude = longitude;
        }
        public double Altitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
