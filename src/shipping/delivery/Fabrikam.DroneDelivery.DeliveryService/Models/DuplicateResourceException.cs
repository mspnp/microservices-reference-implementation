// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Fabrikam.DroneDelivery.DeliveryService.Models
{
    public class DuplicateResourceException : ArgumentException
    {
        public DuplicateResourceException(string message, Exception ex): base(message, ex)
        {

        }
    }
}
