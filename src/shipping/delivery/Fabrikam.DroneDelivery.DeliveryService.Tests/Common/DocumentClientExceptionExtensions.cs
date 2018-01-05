// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.Documents;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;

namespace Fabrikam.DroneDelivery.DeliveryService.Tests.Common
{
    public static class DocumentClientExceptionExtensions
    {
        public static DocumentClientException CreateDocumentClientExceptionForTesting(this Error error, HttpStatusCode httpStatusCode)
        {
            var type = typeof(DocumentClientException);

            var constructor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                 .First(c => c.GetParameters()
                     .All(p => (p.Position == 0 && p.ParameterType == typeof(Error))
                         || (p.Position == 1 && p.ParameterType == typeof(HttpResponseHeaders))
                         || (p.Position == 2 && p.ParameterType == typeof(HttpStatusCode?))
                     )
                 );

            object instance = constructor.Invoke(new object[] {
            error,
            default(HttpResponseHeaders),
            httpStatusCode});

            var documentClientException = (DocumentClientException)instance;

            return documentClientException;
        }
    }
}
