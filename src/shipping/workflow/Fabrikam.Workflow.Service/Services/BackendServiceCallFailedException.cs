// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Fabrikam.Workflow.Service.Services
{
    public class BackendServiceCallFailedException : Exception
    {
        public BackendServiceCallFailedException()
        {
        }

        public BackendServiceCallFailedException(string message) : base(message)
        {
        }

        public BackendServiceCallFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
