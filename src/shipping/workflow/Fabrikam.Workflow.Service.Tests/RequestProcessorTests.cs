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
        private readonly Mock<IPackageServiceCaller> _packageServiceCallerMock;
        private readonly Mock<IDroneSchedulerServiceCaller> _droneSchedulerServiceCallerMock;
        private readonly Mock<IDeliveryServiceCaller> _deliveryServiceCallerMock;
        private readonly RequestProcessor _processor;

        public RequestProcessorTests()
        {
            var servicesBuilder = new ServiceCollection();
            servicesBuilder.AddLogging(logging => logging.AddDebug());
            var services = servicesBuilder.BuildServiceProvider();

            _packageServiceCallerMock = new Mock<IPackageServiceCaller>();
            _droneSchedulerServiceCallerMock = new Mock<IDroneSchedulerServiceCaller>();
            _deliveryServiceCallerMock = new Mock<IDeliveryServiceCaller>();

            _processor =
                new RequestProcessor(
                    services.GetService<ILogger<RequestProcessor>>(),
                    _packageServiceCallerMock.Object,
                    _droneSchedulerServiceCallerMock.Object,
                    _deliveryServiceCallerMock.Object);
        }

        [Fact]
        public async Task WhenInvokingPackageServiceThrows_ProcessingFails()
        {
            _packageServiceCallerMock
                .Setup(c => c.UpsertPackageAsync(It.IsAny<PackageInfo>()))
                .ThrowsAsync(new Exception()).Verifiable();

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.False(success);
            _packageServiceCallerMock.Verify();
            _droneSchedulerServiceCallerMock.VerifyNoOtherCalls();
            _deliveryServiceCallerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WhenInvokingPackageServiceFails_ProcessingFails()
        {
            _packageServiceCallerMock
                .Setup(c => c.UpsertPackageAsync(It.IsAny<PackageInfo>()))
                .ReturnsAsync(default(PackageGen)).Verifiable();

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.False(success);
            _packageServiceCallerMock.Verify();
            _droneSchedulerServiceCallerMock.VerifyNoOtherCalls();
            _deliveryServiceCallerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WhenInvokingDroneSchedulerThrows_ProcessingFails()
        {
            _packageServiceCallerMock
                .Setup(c => c.UpsertPackageAsync(It.IsAny<PackageInfo>()))
                .ReturnsAsync(new PackageGen { Id = "someid" }).Verifiable();
            _droneSchedulerServiceCallerMock
                .Setup(c => c.GetDroneIdAsync(It.IsAny<Delivery>()))
                .ThrowsAsync(new Exception()).Verifiable();

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.False(success);
            _packageServiceCallerMock.Verify();
            _droneSchedulerServiceCallerMock.Verify();
            _deliveryServiceCallerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WhenInvokingDroneSchedulerFails_ProcessingFails()
        {
            _packageServiceCallerMock
                .Setup(c => c.UpsertPackageAsync(It.IsAny<PackageInfo>()))
                .ReturnsAsync(new PackageGen { Id = "someid" }).Verifiable();
            _droneSchedulerServiceCallerMock
                .Setup(c => c.GetDroneIdAsync(It.IsAny<Delivery>()))
                .ReturnsAsync(default(string)).Verifiable();

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.False(success);
            _packageServiceCallerMock.Verify();
            _droneSchedulerServiceCallerMock.Verify();
            _deliveryServiceCallerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WhenInvokingDeliverySchedulerThrows_ProcessingFails()
        {
            _packageServiceCallerMock
                .Setup(c => c.UpsertPackageAsync(It.IsAny<PackageInfo>()))
                .ReturnsAsync(new PackageGen { Id = "someid" }).Verifiable();
            _droneSchedulerServiceCallerMock
                .Setup(c => c.GetDroneIdAsync(It.IsAny<Delivery>()))
                .ReturnsAsync("droneId").Verifiable();
            _deliveryServiceCallerMock
                .Setup(c => c.ScheduleDeliveryAsync(It.IsAny<Delivery>(), "droneId"))
                .ThrowsAsync(new Exception()).Verifiable();

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.False(success);
            _packageServiceCallerMock.Verify();
            _droneSchedulerServiceCallerMock.Verify();
            _deliveryServiceCallerMock.Verify();
        }

        [Fact]
        public async Task WhenInvokingDeliverySchedulerFails_ProcessingFails()
        {
            _packageServiceCallerMock
                .Setup(c => c.UpsertPackageAsync(It.IsAny<PackageInfo>()))
                .ReturnsAsync(new PackageGen { Id = "someid" }).Verifiable();
            _droneSchedulerServiceCallerMock
                .Setup(c => c.GetDroneIdAsync(It.IsAny<Delivery>()))
                .ReturnsAsync("droneId").Verifiable();
            _deliveryServiceCallerMock
                .Setup(c => c.ScheduleDeliveryAsync(It.IsAny<Delivery>(), "droneId"))
                .ReturnsAsync(default(DeliverySchedule)).Verifiable();

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.False(success);
            _packageServiceCallerMock.Verify();
            _droneSchedulerServiceCallerMock.Verify();
            _deliveryServiceCallerMock.Verify();
        }

        [Fact]
        public async Task WhenProcessingAValidDelivery_ProcessingSucceeds()
        {
            _packageServiceCallerMock
                .Setup(c => c.UpsertPackageAsync(It.IsAny<PackageInfo>()))
                .ReturnsAsync(new PackageGen { Id = "someid" }).Verifiable();
            _droneSchedulerServiceCallerMock
                .Setup(c => c.GetDroneIdAsync(It.IsAny<Delivery>()))
                .ReturnsAsync("droneId").Verifiable();
            _deliveryServiceCallerMock
                .Setup(c => c.ScheduleDeliveryAsync(It.IsAny<Delivery>(), "droneId"))
                .ReturnsAsync(new DeliverySchedule { Id = "someDeliveryId" }).Verifiable();

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _processor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.True(success);
            _packageServiceCallerMock.Verify();
            _droneSchedulerServiceCallerMock.Verify();
            _deliveryServiceCallerMock.Verify();
        }
    }
}
