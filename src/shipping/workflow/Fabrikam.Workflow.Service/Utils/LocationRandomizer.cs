// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Fabrikam.Workflow.Service.Models;

namespace Fabrikam.Workflow.Service.Utils
{
    static class LocationRandomizer
    {
        private static Random Random = new Random();

        public static Location GetRandomLocation()
        {
            Location location = new Location();
            lock (Random)
            {
                location.Altitude = Random.NextDouble();
                location.Latitude = Random.NextDouble();
                location.Longitude = Random.NextDouble();
            }

            return location;
        }
    }
}
