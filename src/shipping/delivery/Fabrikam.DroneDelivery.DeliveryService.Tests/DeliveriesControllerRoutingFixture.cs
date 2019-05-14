// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fabrikam.DroneDelivery.Common;
using Fabrikam.DroneDelivery.DeliveryService.Middlewares.Builder;
using Fabrikam.DroneDelivery.DeliveryService.Models;
using Fabrikam.DroneDelivery.DeliveryService.Services;
using Moq;

namespace Fabrikam.DroneDelivery.DeliveryService.Tests
{
    [TestClass]
    public class DeliveriesControllerRoutingFixture
    {
        private readonly TestServer _testServer;
        private readonly Mock<IDeliveryRepository> _deliveryRepositoryMock;
        private readonly Mock<INotifyMeRequestRepository> _notifyMeRequestRepository;
        private readonly Mock<INotificationService> _notificationService;
        private readonly Mock<IDeliveryTrackingEventRepository> _deliveryTrackingRepository;

        public DeliveriesControllerRoutingFixture()
        {
            _deliveryRepositoryMock = new Mock<IDeliveryRepository>();
            _notifyMeRequestRepository = new Mock<INotifyMeRequestRepository>();
            _notificationService = new Mock<INotificationService>();
            _deliveryTrackingRepository = new Mock<IDeliveryTrackingEventRepository>();

            _testServer =
                new TestServer(
                    new WebHostBuilder()
                        .Configure(builder =>
                        {
                            builder.UseGlobalExceptionHandler();
                            builder.UseMvc();
                        })
                        .ConfigureServices(builder =>
                        {
                            builder.AddMvc();

                            builder.AddSingleton(_deliveryRepositoryMock.Object);
                            builder.AddSingleton(_notifyMeRequestRepository.Object);
                            builder.AddSingleton(_notificationService.Object);
                            builder.AddSingleton(_deliveryTrackingRepository.Object);
                        }));
        }

        [TestCleanup]
        public void TearDown()
        {
            _testServer?.Dispose();
        }

        [TestMethod]
        public async Task GetDelivery_GetsResponse()
        {
            var deliveryId = Guid.NewGuid().ToString();

            _deliveryRepositoryMock
                .Setup(r => r.GetAsync(deliveryId))
                .ReturnsAsync(new InternalDelivery(deliveryId, null, null, null, null, false, ConfirmationType.None, null))
                .Verifiable();

            using (var client = _testServer.CreateClient())
            {
                var response = await client.GetAsync($"http://localhost/api/deliveries/{deliveryId}");
                response.EnsureSuccessStatusCode();

                var delivery = await response.Content.ReadAsAsync<Delivery>();

                Assert.AreEqual(deliveryId, delivery.Id);
            }

            _deliveryRepositoryMock.VerifyAll();
        }

        [TestMethod]
        public async Task GetDeliveryThroughPublicRoute_GetsResponse()
        {
            var deliveryId = Guid.NewGuid().ToString();

            _deliveryRepositoryMock
                .Setup(r => r.GetAsync(deliveryId))
                .ReturnsAsync(new InternalDelivery(deliveryId, null, null, null, null, false, ConfirmationType.None, null))
                .Verifiable();

            using (var client = _testServer.CreateClient())
            {
                var response = await client.GetAsync($"http://localhost/api/deliveries/public/{deliveryId}");
                response.EnsureSuccessStatusCode();

                var delivery = await response.Content.ReadAsAsync<Delivery>();

                Assert.AreEqual(deliveryId, delivery.Id);
            }

            _deliveryRepositoryMock.VerifyAll();
        }

        [TestMethod]
        public async Task PutDelivery_GetsResponse()
        {
            var deliveryId = Guid.NewGuid().ToString();
            var delivery = new Delivery(deliveryId, new UserAccount("user", "accound"), null, null, null, false, ConfirmationType.None, null);

            _deliveryRepositoryMock
                .Setup(r => r.CreateAsync(It.Is<InternalDelivery>(d => d.Id == deliveryId)))
                .Returns(Task.CompletedTask)
                .Verifiable();
            _deliveryTrackingRepository
                .Setup(r => r.AddAsync(It.Is<DeliveryTrackingEvent>(e => e.DeliveryId == deliveryId)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            using (var client = _testServer.CreateClient())
            {
                var response = await client.PutAsJsonAsync($"http://localhost/api/deliveries/{deliveryId}", delivery);
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

                var createdDelivery = await response.Content.ReadAsAsync<Delivery>();
                Assert.AreEqual(deliveryId, delivery.Id);
            }

            _deliveryRepositoryMock.VerifyAll();
            _deliveryTrackingRepository.VerifyAll();
        }

        [TestMethod]
        public async Task PutDeliveryThroughPublicRoute_GetsNotFoundResponse()
        {
            var deliveryId = Guid.NewGuid().ToString();
            var delivery = new Delivery(deliveryId, new UserAccount("user", "accound"), null, null, null, false, ConfirmationType.None, null);

            using (var client = _testServer.CreateClient())
            {
                var response = await client.PutAsJsonAsync($"http://localhost/api/deliveries/public/{deliveryId}", delivery);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            }

            _deliveryRepositoryMock.VerifyAll();
            _deliveryTrackingRepository.VerifyAll();
        }

        [TestMethod]
        public async Task GetOwner_GetsResponse()
        {
            var deliveryId = Guid.NewGuid().ToString();

            _deliveryRepositoryMock
                .Setup(r => r.GetAsync(deliveryId))
                .ReturnsAsync(new InternalDelivery(deliveryId, new UserAccount("user", "account"), null, null, null, false, ConfirmationType.None, null))
                .Verifiable();

            using (var client = _testServer.CreateClient())
            {
                var response = await client.GetAsync($"http://localhost/api/deliveries/{deliveryId}/owner");
                response.EnsureSuccessStatusCode();

                var userAccount = await response.Content.ReadAsAsync<UserAccount>();

                Assert.AreEqual("user", userAccount.UserId);
            }

            _deliveryRepositoryMock.VerifyAll();
        }

        [TestMethod]
        public async Task GetStatus_GetsResponse()
        {
            var deliveryId = Guid.NewGuid().ToString();

            _deliveryRepositoryMock
                .Setup(r => r.GetAsync(deliveryId))
                .ReturnsAsync(new InternalDelivery(deliveryId, new UserAccount("user", "account"), null, null, null, false, ConfirmationType.None, null))
                .Verifiable();

            using (var client = _testServer.CreateClient())
            {
                var response = await client.GetAsync($"http://localhost/api/deliveries/{deliveryId}/status");
                response.EnsureSuccessStatusCode();

                var status = await response.Content.ReadAsAsync<DeliveryStatus>();

                Assert.AreEqual(DeliveryStage.Created, status.Stage);   // exposes deserialization issue
            }

            _deliveryRepositoryMock.VerifyAll();
        }
    }
}
