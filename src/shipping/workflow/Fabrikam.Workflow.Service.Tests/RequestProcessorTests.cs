// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fabrikam.Workflow.Service.Models;
using Fabrikam.Workflow.Service.RequestProcessing;
using Fabrikam.Workflow.Service.Services;
using Moq;
using Xunit;

namespace Fabrikam.Workflow.Service.Tests
{
    public class RequestProcessorTests
    {
        private readonly Mock<IPackageServiceCaller> packageServiceCallerMock;
        private readonly RequestProcessor processor;

        public RequestProcessorTests()
        {
            var servicesBuilder = new ServiceCollection();
            servicesBuilder.AddLogging(logging => logging.AddDebug());
            var services = servicesBuilder.BuildServiceProvider();

            packageServiceCallerMock = new Mock<IPackageServiceCaller>();

            processor = new RequestProcessor(services.GetService<ILogger<RequestProcessor>>(), packageServiceCallerMock.Object);
        }

        [Fact]
        public async Task WhenInvokingPackageServiceThrows_ProcessingFails()
        {
            var delivery =
                new Delivery
                {
                    DeliveryId = "delivery",
                    PackageInfo = new PackageInfo { PackageId = "package", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };

            packageServiceCallerMock.Setup(c => c.CreatePackageAsync(It.IsAny<PackageInfo>())).ThrowsAsync(new Exception()).Verifiable();

            var success = await processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            packageServiceCallerMock.Verify();
            Assert.False(success);
        }

        [Fact]
        public async Task WhenInvokingPackageServiceFails_ProcessingFails()
        {
            var delivery =
                new Delivery
                {
                    DeliveryId = "delivery",
                    PackageInfo = new PackageInfo { PackageId = "package", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };

            packageServiceCallerMock.Setup(c => c.CreatePackageAsync(It.IsAny<PackageInfo>())).ReturnsAsync(default(PackageGen)).Verifiable();

            var success = await processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            packageServiceCallerMock.Verify();
            Assert.False(success);
        }

        [Fact]
        public async Task WhenProcessingAValidDelivery_ProcessingSucceeds()
        {
            var delivery =
                new Delivery
                {
                    DeliveryId = "delivery",
                    PackageInfo = new PackageInfo { PackageId = "package", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };

            packageServiceCallerMock.Setup(c => c.CreatePackageAsync(It.IsAny<PackageInfo>())).ReturnsAsync(new PackageGen { Id = "someid" }).Verifiable();

            var success = await processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            packageServiceCallerMock.Verify();
            Assert.True(success);
        }
    }
}
