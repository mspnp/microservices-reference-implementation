// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Fabrikam.DroneDelivery.Common;
using Fabrikam.DroneDelivery.DeliveryService.Controllers;
using Fabrikam.DroneDelivery.DeliveryService.Services;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Tests
{
    [TestClass]
    public class DeliveriesControllerFixture
    {
        private static UserAccount userAccount = new UserAccount("userid", "accountid");

        private static InternalDelivery delivery = new InternalDelivery("deliveryid",
                                        userAccount,
                                        new Location(0, 0, 0),
                                        new Location(1, 1, 1),
                                        "deadline",
                                        false,
                                        ConfirmationType.FingerPrint,
                                        "droneid");
        [TestMethod]
        public async Task Get_Returns404_IfDeliveryIdNotValid()
        {
            // Arrange
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(new Mock<IDeliveryRepository>().Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);
            
            // Act
            var result = await target.Get("invaliddeliveryid") as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task GetOwner_Returns404_IfDeliveryIdNotValid()
        {
            // Arrange
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(new Mock<IDeliveryRepository>().Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);

            // Act
            var result = await target.GetOwner("invaliddeliveryid") as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task GetOwner_ReturnsOwner()
        {
            // Arrange
            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetAsync(It.IsAny<string>()))
                                .ReturnsAsync(delivery);

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);
            
            // Act
            var result = await target.GetOwner("deliveryid") as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userAccount, result.Value);
        }

        [TestMethod]
        public async Task GetStatus_Returns404_IfDeliveryIdNotValid()
        {
            // Arrange
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(new Mock<IDeliveryRepository>().Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);

            // Act
            var result = await target.GetStatus("invaliddeliveryid") as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task Put_Returns204_IfDeliveryIdExists()
        {
            // Arrange
            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.CreateAsync(It.IsAny<InternalDelivery>())).Throws(new DuplicateResourceException("dupe", null));
            deliveryRepository.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<InternalDelivery>())).Returns(Task.CompletedTask);

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                         .Returns(new Mock<ILogger<DeliveriesController>>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);

            // Act
            var result = await target.Put(new Delivery("existingdeliveryid", new UserAccount("user", "account"), new Location(0, 0, 0), new Location(2, 2, 2), "deadline", true, ConfirmationType.FingerPrint, "drone"), "existingdeliveryid");
            var statusCodeResult = (StatusCodeResult)result;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(204, statusCodeResult.StatusCode);
        }

        [TestMethod]
        public async Task Put_AddsToCache()
        {
            // Arrange
            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.CreateAsync(It.IsAny<InternalDelivery>())).Returns(Task.CompletedTask);

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);
            // Act
            var result = await target.Put(new Delivery("deliveryid", new UserAccount("user", "account"), new Location(0, 0, 0), new Location(2, 2, 2), "deadline", true, ConfirmationType.FingerPrint, "drone"), "deliveryid");
            var createdAtRouteResult = (CreatedAtRouteResult)result;

            // Assert
            Assert.AreEqual(201, createdAtRouteResult.StatusCode);
            Assert.IsNotNull(createdAtRouteResult.Value);
            deliveryRepository.VerifyAll();
        }

        [TestMethod]
        public async Task Put_AddsDeliveryStartedEvent()
        {
            // Arrange
            DeliveryStatusEvent startedEvent = null;
            var deliveryStatusEventRepository = new Mock<IDeliveryStatusEventRepository>();
            deliveryStatusEventRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryStatusEvent>()))
                                         .Returns(Task.CompletedTask)
                                         .Callback<DeliveryStatusEvent>(e => startedEvent = e);

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(new Mock<IDeliveryRepository>().Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  deliveryStatusEventRepository.Object,
                                                  loggerFactory.Object);
            // Act
            var result = await target.Put(new Delivery("deliveryid", new UserAccount("user", "account"), new Location(0, 0, 0), new Location(2, 2, 2), "deadline", true, ConfirmationType.FingerPrint, "drone"), "deliveryid");
            var createdAtRouteResult = (CreatedAtRouteResult)result;

            // Assert
            Assert.AreEqual(201, createdAtRouteResult.StatusCode);
            Assert.IsNotNull(createdAtRouteResult.Value);
            Assert.IsNotNull(startedEvent);
            Assert.AreEqual(DeliveryEventType.Created, startedEvent.Stage);
            deliveryStatusEventRepository.VerifyAll();
        }

        [TestMethod]
        public async Task Patch_Returns404_IfDeliveryIdNotValid()
        {
            // Arrange
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(new Mock<IDeliveryRepository>().Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);

            // Act
            var result = await target.Patch("invaliddeliveryid", null) as NotFoundResult;


            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task Patch_UpdatesCache()
        {
            // Arrange
            InternalDelivery updatedDelivery = null;
            var rescheduledDelivery = new RescheduledDelivery(new Location(2, 2, 2),
                                                              new Location(3, 3, 3),
                                                              "newdeadline");

            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetAsync("deliveryid")).ReturnsAsync(delivery);
            deliveryRepository.Setup(r => r.UpdateAsync("deliveryid", It.IsAny<InternalDelivery>()))
                              .Returns(Task.CompletedTask)
                              .Callback((string i, InternalDelivery d) => updatedDelivery = d);

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);

            // Act
            await target.Patch("deliveryid", rescheduledDelivery);

            // Assert
            //unchanged values
            Assert.AreEqual("deliveryid", updatedDelivery.Id);
            Assert.AreEqual("userid", updatedDelivery.Owner.UserId);

            //updated values
            Assert.AreEqual(2, updatedDelivery.Pickup.Altitude);
            Assert.AreEqual(3, updatedDelivery.Dropoff.Altitude);
            Assert.AreEqual("newdeadline", updatedDelivery.Deadline);

            deliveryRepository.VerifyAll();
        }

        [TestMethod]
        public async Task Patch_AddsRescheduledDeliveryEvent()
        {
            // Arrange
            DeliveryStatusEvent rescheduledEvent = null;

            var rescheduledDelivery = new RescheduledDelivery(new Location(2, 2, 2),
                                                              new Location(3, 3, 3),
                                                              "newdeadline");

            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetAsync("deliveryid")).ReturnsAsync(delivery);

            var deliveryStatusEventRepository = new Mock<IDeliveryStatusEventRepository>();
            deliveryStatusEventRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryStatusEvent>()))
                                         .Returns(Task.CompletedTask)
                                         .Callback<DeliveryStatusEvent>(e => rescheduledEvent = e);

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  deliveryStatusEventRepository.Object,
                                                  loggerFactory.Object);

            // Act
            var result = await target.Patch("deliveryid", rescheduledDelivery) as OkResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(rescheduledEvent);
            Assert.AreEqual(DeliveryEventType.Rescheduled, rescheduledEvent.Stage);
            deliveryStatusEventRepository.VerifyAll();
        }

        [TestMethod]
        public async Task Delete_Returns404_IfDeliveryIdNotValid()
        {
            // Arrange
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(new Mock<IDeliveryRepository>().Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);

            // Act
            var result = await target.Delete("invaliddeliveryid") as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task Delete_SendsMessageWithCancelledEvent()
        {
            // Arrange
            InternalDelivery cancelledDelivery = null;
            DeliveryStatusEvent cancelEvent = null;
            DeliveryStatusEvent[] allEvents = null;

            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetAsync("deliveryid")).ReturnsAsync(delivery);

            var deliveryHistoryService = new Mock<IDeliveryHistoryService>();
            deliveryHistoryService.Setup(r => r.CancelAsync(It.IsAny<InternalDelivery>(), It.IsAny<DeliveryStatusEvent[]>()))
                      .Returns(Task.CompletedTask)
                      .Callback<InternalDelivery, DeliveryStatusEvent[]>((d, es) =>
                      {
                          cancelledDelivery = d;
                          allEvents = es;
                      });

            var deliveryStatusEventRepository = new Mock<IDeliveryStatusEventRepository>();
            deliveryStatusEventRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryStatusEvent>()))
                                         .Returns(Task.CompletedTask)
                                         .Callback<DeliveryStatusEvent>(e => cancelEvent = e);

            deliveryStatusEventRepository.Setup(r => r.GetByDeliveryIdAsync("deliveryid"))
                                         .ReturnsAsync(new ReadOnlyCollection<DeliveryStatusEvent>(new List<DeliveryStatusEvent>() { cancelEvent }));

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  deliveryHistoryService.Object,
                                                  deliveryStatusEventRepository.Object,
                                                  loggerFactory.Object);
            // Act
            await target.Delete("deliveryid");

            // Assert
            Assert.IsNotNull(cancelEvent);
            Assert.AreEqual("deliveryid", cancelEvent.DeliveryId);
            Assert.AreEqual(DeliveryEventType.Cancelled, cancelEvent.Stage);
            deliveryRepository.VerifyAll();
            deliveryHistoryService.Verify(s => s.CancelAsync(delivery, allEvents), Times.Once);
        }

        [TestMethod]
        public async Task NotifyMe_Returns404_IfDeliveryIdNotValid()
        {
            // Arrange
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(new Mock<IDeliveryRepository>().Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);

            // Act
            var result = await target.NotifyMe("invaliddeliveryid", null) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task NotifyMe_AddsNotifyMeRequest()
        {
            // Arrange
            InternalNotifyMeRequest savedNotifyMeRequest = null;

            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetAsync("deliveryid")).ReturnsAsync(delivery);

            var notifyMeRequestRepository = new Mock<INotifyMeRequestRepository>();
            notifyMeRequestRepository.Setup(r => r.AddAsync(It.IsAny<InternalNotifyMeRequest>()))
                                     .Returns(Task.CompletedTask)
                                     .Callback<InternalNotifyMeRequest>(r => savedNotifyMeRequest = r);

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  notifyMeRequestRepository.Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);

            var notifyMeRequest = new NotifyMeRequest("email@test.com", "1234567");
            
            // Act
            var result = await target.NotifyMe("deliveryid", notifyMeRequest) as NoContentResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("deliveryid", savedNotifyMeRequest.DeliveryId);
            Assert.AreEqual("email@test.com", savedNotifyMeRequest.EmailAddress);
            Assert.AreEqual("1234567", savedNotifyMeRequest.SMS);
        }

        [TestMethod]
        public async Task Confirm_Returns404_IfDeliveryIdNotValid()
        {
            // Arrange
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(new Mock<IDeliveryRepository>().Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  new Mock<IDeliveryStatusEventRepository>().Object,
                                                  loggerFactory.Object);
            // Act
            var result = await target.Confirm("invaliddeliveryid", null) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task Confirm_SendsNotifications()
        {
            // Arrange
            DeliveryStatusEvent completeEvent = null;

            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetAsync("deliveryid")).ReturnsAsync(delivery);

            var notifyMeRequests = new List<InternalNotifyMeRequest>();
            notifyMeRequests.Add(new InternalNotifyMeRequest
            { DeliveryId = "deliveryid", EmailAddress = "email1@test.com", SMS = "1111111" });
            notifyMeRequests.Add(new InternalNotifyMeRequest
            { DeliveryId = "deliveryid", EmailAddress = "email2@test.com", SMS = "2222222" });
            var notifyMeRequestRepository = new Mock<INotifyMeRequestRepository>();
            notifyMeRequestRepository.Setup(r => r.GetAllByDeliveryIdAsync("deliveryid"))
                                     .ReturnsAsync(notifyMeRequests);

            int notificationServiceCalled = 0;
            var notificationService = new Mock<INotificationService>();
            notificationService.Setup(s => s.SendNotificationsAsync(It.IsAny<InternalNotifyMeRequest>()))
                               .Returns(Task.CompletedTask)
                               .Callback(() => notificationServiceCalled++);


            var deliveryStatusEventRepository = new Mock<IDeliveryStatusEventRepository>();

            deliveryStatusEventRepository.Setup(r => r.GetByDeliveryIdAsync("deliveryid"))
                                         .ReturnsAsync(new ReadOnlyCollection<DeliveryStatusEvent>(new List<DeliveryStatusEvent>() { completeEvent }));

            var deliveryHistoryService = new Mock<IDeliveryHistoryService>();
            deliveryHistoryService.Setup(r => r.CompleteAsync(delivery, It.IsAny<InternalConfirmation>(), It.IsAny<DeliveryStatusEvent[]>())).Returns(Task.CompletedTask);

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  notifyMeRequestRepository.Object,
                                                  notificationService.Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  deliveryStatusEventRepository.Object,
                                                  loggerFactory.Object);

            var confirmation = new Confirmation(new DateTimeStamp("datetimevalue"),
                                                                    new Location(1, 2, 3),
                                                                    ConfirmationType.Picture,
                                                                    "confirmationblob");
            // Acts
            var result = await target.Confirm("deliveryid", confirmation) as OkResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, notificationServiceCalled);
        }

        [TestMethod]
        public async Task Confirm_SendsMessageCompleteToDeliveryHistory()
        {
            // Arrange
            InternalDelivery confirmedDelivery = null;
            InternalConfirmation sentConfirmation = null;
            DeliveryStatusEvent completeEvent = null;
            DeliveryStatusEvent[] allEvents = null;

            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetAsync("deliveryid")).ReturnsAsync(delivery);

            var notifyMeRequestRepository = new Mock<INotifyMeRequestRepository>();
            notifyMeRequestRepository.Setup(r => r.GetAllByDeliveryIdAsync("deliveryid"))
                                     .ReturnsAsync(new List<InternalNotifyMeRequest>());

            var deliveryStatusEventRepository = new Mock<IDeliveryStatusEventRepository>();
            deliveryStatusEventRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryStatusEvent>()))
                                         .Returns(Task.CompletedTask)
                                         .Callback<DeliveryStatusEvent>(e => completeEvent = e);

            deliveryStatusEventRepository.Setup(r => r.GetByDeliveryIdAsync("deliveryid"))
                                         .ReturnsAsync(new ReadOnlyCollection<DeliveryStatusEvent>(new List<DeliveryStatusEvent>() { completeEvent }));

            var deliveryHistoryService = new Mock<IDeliveryHistoryService>();
            deliveryHistoryService.Setup(r => r.CompleteAsync(It.IsAny<InternalDelivery>(), It.IsAny<InternalConfirmation>(), It.IsAny<DeliveryStatusEvent[]>()))
                                  .Returns(Task.CompletedTask)
                                  .Callback<InternalDelivery, InternalConfirmation, DeliveryStatusEvent[]>((d, c, es) =>
                                  {
                                      confirmedDelivery = d;
                                      sentConfirmation = c;
                                      allEvents = es;
                                  });

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  notifyMeRequestRepository.Object,
                                                  new Mock<INotificationService>().Object,
                                                  deliveryHistoryService.Object,
                                                  deliveryStatusEventRepository.Object,
                                                  loggerFactory.Object);

            var confirmation = new Confirmation(new DateTimeStamp("datetimevalue"),
                                                        new Location(1, 2, 3),
                                                        ConfirmationType.Picture,
                                                        "confirmationblob");

            // Act
            var result = await target.Confirm("deliveryid", confirmation) as OkResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("datetimevalue", sentConfirmation.DateTime.DateTimeValue);
            Assert.AreEqual(1, sentConfirmation.GeoCoordinates.Altitude);
            Assert.AreEqual(2, sentConfirmation.GeoCoordinates.Latitude);
            Assert.AreEqual(3, sentConfirmation.GeoCoordinates.Longitude);
            Assert.AreEqual(ConfirmationType.Picture, sentConfirmation.ConfirmationType);
            Assert.AreEqual("confirmationblob", sentConfirmation.ConfirmationBlob);
            deliveryHistoryService.Verify(s => s.CompleteAsync(confirmedDelivery, It.IsAny<InternalConfirmation>(), allEvents), Times.Once);
        }

        [TestMethod]
        public async Task Confirm_DeletesDeliveryLogically()
        {
            // Arrange
            InternalDelivery confirmedDelivery = null;
            DeliveryStatusEvent completeEvent = null;

            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetAsync("deliveryid")).ReturnsAsync(delivery);
            deliveryRepository.Setup(r => r.DeleteAsync("deliveryid", It.IsAny<InternalDelivery>()))
                            .Returns(Task.CompletedTask)
                            .Callback<string, InternalDelivery>((i, d) => confirmedDelivery = d);

            var deliveryStatusEventRepository = new Mock<IDeliveryStatusEventRepository>();
            deliveryStatusEventRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryStatusEvent>()))
                                         .Returns(Task.CompletedTask)
                                         .Callback<DeliveryStatusEvent>(e => completeEvent = e);

            deliveryStatusEventRepository.Setup(r => r.GetByDeliveryIdAsync("deliveryid"))
                                         .ReturnsAsync(new ReadOnlyCollection<DeliveryStatusEvent>(new List<DeliveryStatusEvent>() { completeEvent }));

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  deliveryStatusEventRepository.Object,
                                                  loggerFactory.Object);
            var location = new Location(1, 2, 3);
            var confirmation = new Confirmation(new DateTimeStamp("datetimevalue"),
                                                        location,
                                                        ConfirmationType.Picture,
                                                        "confirmationblob");

            // Act
            var result = await target.Confirm("deliveryid", confirmation) as OkResult;

            // Assert
            Assert.IsNotNull(result);
            deliveryRepository.Verify(r => r.DeleteAsync("deliveryid", confirmedDelivery));
            Assert.AreEqual(location, confirmedDelivery.Dropoff);
        }

        [TestMethod]
        public async Task Confirm_AddsDeliveryCompleteEvent()
        {
            // Arrange
            DeliveryStatusEvent completeEvent = null;

            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetAsync("deliveryid")).ReturnsAsync(delivery);

            var deliveryStatusEventRepository = new Mock<IDeliveryStatusEventRepository>();
            deliveryStatusEventRepository.Setup(r => r.AddAsync(It.IsAny<DeliveryStatusEvent>()))
                                         .Returns(Task.CompletedTask)
                                         .Callback<DeliveryStatusEvent>(e => completeEvent = e);

            deliveryStatusEventRepository.Setup(r => r.GetByDeliveryIdAsync("deliveryid"))
                                         .ReturnsAsync(new ReadOnlyCollection<DeliveryStatusEvent>(new List<DeliveryStatusEvent>() { completeEvent }));

            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var target = new DeliveriesController(deliveryRepository.Object,
                                                  new Mock<INotifyMeRequestRepository>().Object,
                                                  new Mock<INotificationService>().Object,
                                                  new Mock<IDeliveryHistoryService>().Object,
                                                  deliveryStatusEventRepository.Object,
                                                  loggerFactory.Object);

            var confirmation = new Confirmation(new DateTimeStamp("datetimevalue"),
                                                        new Location(1, 2, 3),
                                                        ConfirmationType.Picture,
                                                        "confirmationblob");

            // Act
            var result = await target.Confirm("deliveryid", confirmation) as OkResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(completeEvent);
            Assert.AreEqual(DeliveryEventType.DeliveryComplete, completeEvent.Stage);
        }

    }
}
