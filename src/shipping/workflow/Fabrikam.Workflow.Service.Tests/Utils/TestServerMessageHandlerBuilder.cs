// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Http;

namespace Fabrikam.Workflow.Service.Tests.Utils
{
    public class TestServerMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        public TestServerMessageHandlerBuilder(TestServer testServer)
        {
            AdditionalHandlers = new List<DelegatingHandler>();
            PrimaryHandler = testServer.CreateHandler();
        }

        public override string Name { get; set; }

        public override HttpMessageHandler PrimaryHandler { get; set; }

        public override IList<DelegatingHandler> AdditionalHandlers { get; }

        public override HttpMessageHandler Build() => CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
    }
}
