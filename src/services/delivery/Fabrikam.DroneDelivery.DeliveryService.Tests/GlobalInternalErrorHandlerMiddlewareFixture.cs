// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Fabrikam.DroneDelivery.DeliveryService.Middlewares;
using Fabrikam.DroneDelivery.DeliveryService.Tests.Common;


namespace Fabrikam.DroneDelivery.DeliveryService.Tests
{
    [TestClass]
    public class GlobalInternalErrorHandlerMiddlewareFixture
    {
        [TestMethod]
        public async Task IfInternalServerErrorOccurs_ItHandlesTheException()
        {
            // Arrange

            // Options
            var optionsMock = new Mock<IOptions<ExceptionHandlerOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new ExceptionHandlerOptions());

            // Request
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Scheme).Returns("http");
            requestMock.Setup(x => x.Host).Returns(new HostString("localhost"));
            requestMock.Setup(x => x.Path).Returns(new PathString("/FooBar"));
            requestMock.Setup(x => x.PathBase).Returns(new PathString("/"));
            requestMock.Setup(x => x.Method).Returns("GET");
            requestMock.Setup(x => x.Body).Returns(new MemoryStream());

            // Context
            var contextMock = new Mock<HttpContext>();

            // Response
            var responseMock = new Mock<HttpResponse>();
            responseMock.SetupAllProperties();
            responseMock.SetupGet(y => y.Headers).Returns(new HeaderDictionary());
            responseMock.SetupGet(y => y.Body).Returns(new MemoryStream());
            responseMock.SetupGet(y => y.HttpContext).Returns(contextMock.Object);
            // Features
            var featuresCollection = new FeatureCollection();
            featuresCollection.Set<IHttpResponseFeature>(new HttpResponseFeature());

            // Context Setup
            contextMock.Setup(z => z.Request).Returns(requestMock.Object);
            contextMock.Setup(z => z.Response).Returns(responseMock.Object);
            contextMock.Setup(z => z.Features).Returns(featuresCollection);

            // Middleware
            var logRequestMiddleware = new GlobalInternalErrorHandlerMiddleware(next: Next, options: optionsMock.Object);

            // Act
            await logRequestMiddleware.Invoke(contextMock.Object);

            // Assert
            responseMock.VerifySet(r500 => r500.StatusCode = 500);
            Assert.IsNotNull(featuresCollection.Get<IExceptionHandlerFeature>());
            Assert.IsNotNull(featuresCollection.Get<IExceptionHandlerFeature>().Error);
        }

        [TestMethod]
        public async Task IfRequestRateTooLargeException_ItHandlesTheException()
        {
            // Arrange

            // Options
            var optionsMock = new Mock<IOptions<ExceptionHandlerOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new ExceptionHandlerOptions());

            // Request
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Scheme).Returns("http");
            requestMock.Setup(x => x.Host).Returns(new HostString("localhost"));
            requestMock.Setup(x => x.Path).Returns(new PathString("/FooBar"));
            requestMock.Setup(x => x.PathBase).Returns(new PathString("/"));
            requestMock.Setup(x => x.Method).Returns("GET");
            requestMock.Setup(x => x.Body).Returns(new MemoryStream());

            // Context
            var contextMock = new Mock<HttpContext>();

            // Response
            var responseMock = new Mock<HttpResponse>();
            responseMock.SetupAllProperties();
            responseMock.SetupGet(y => y.Headers).Returns(new HeaderDictionary());
            responseMock.SetupGet(y => y.Body).Returns(new MemoryStream());
            responseMock.SetupGet(y => y.HttpContext).Returns(contextMock.Object);
            // Features
            var featuresCollection = new FeatureCollection();
            featuresCollection.Set<IHttpResponseFeature>(new HttpResponseFeature());

            // Context Setup
            contextMock.Setup(z => z.Request).Returns(requestMock.Object);
            contextMock.Setup(z => z.Response).Returns(responseMock.Object);
            contextMock.Setup(z => z.Features).Returns(featuresCollection);

            // Middleware
            var logRequestMiddleware = new GlobalInternalErrorHandlerMiddleware(next: NextTooManyRequests, options: optionsMock.Object);

            // Act
            await logRequestMiddleware.Invoke(contextMock.Object);

            // Assert
            responseMock.VerifySet(r500 => r500.StatusCode = 500);
            Assert.IsNotNull(featuresCollection.Get<IExceptionHandlerFeature>());
            Assert.IsNotNull(featuresCollection.Get<IExceptionHandlerFeature>().Error);
        }

        Task NextTooManyRequests(HttpContext context)
        {
            if (context.Response.StatusCode == 429)
            {
                return Task.CompletedTask;
            }
            else
                return Task.FromException(new Error
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = "429",
                    Message = "Request rate is large"
                }.CreateDocumentClientExceptionForTesting((HttpStatusCode)429));
        }

        Task Next(HttpContext context)
        {
            if (context.Response.StatusCode == 500)
            {
                return Task.CompletedTask;
            }
            else
                throw new Exception();
        }
    }
}
