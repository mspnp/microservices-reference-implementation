// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;
using Fabrikam.DroneDelivery.DroneSchedulerService.Services;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Tests
{
    public class InvoicingRepositoryTests
    {
        [Fact]
        public async Task WhenGetAggregatedInvoincingData_ThenInvokesDroneUtilizationRepository()
        {
            // Arrange
            string ownerId = "o00042";
            int year = 2019;
            int month = 6;
            var cosmosDbMock = new Mock<ICosmosRepository<InternalDroneUtilization>>();
            var repo = new InvoicingRepository(cosmosDbMock.Object);

            // Act
            var result = await repo.GetAggreatedInvoincingDataAsync(ownerId, year, month);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0d, result.Item1);
            Assert.Equal(0d, result.Item2);
            cosmosDbMock
                .Verify(p =>
                    p.GetItemsAsync(
                        It.IsAny<Expression<Func<InternalDroneUtilization, bool>>>(),
                        ownerId),
                    Times.Once);
        }

        [Fact]
        public async Task WhenGetAggregatedInvoincingDataForAValidPeriod_ThenRepoReturnsData()
        {
            // Arrange
            string ownerId = "o00042";
            int year = 2019;
            int month = 6;
            var invoicingData = new List<InternalDroneUtilization> {
                new InternalDroneUtilization{
                    TraveledMiles=10d,
                    AssignedHours=1d
                },
                new InternalDroneUtilization{
                    TraveledMiles=32d,
                    AssignedHours=2d
                }
            };
            var cosmosDbMock = new Mock<ICosmosRepository<InternalDroneUtilization>>();
            cosmosDbMock.Setup(r =>
                    r.GetItemsAsync(
                        It.IsAny<Expression<Func<InternalDroneUtilization, bool>>>(),
                        ownerId))
                    .ReturnsAsync(invoicingData.AsEnumerable());
            var repo = new InvoicingRepository(cosmosDbMock.Object);

            // Act
            var (traveledMiles, assignedHours)  =
                await repo.GetAggreatedInvoincingDataAsync(
                        ownerId,
                        year,
                        month);

            // Assert
            Assert.Equal(
                invoicingData.Sum(d => d.TraveledMiles),
                traveledMiles);
            Assert.Equal(
                invoicingData.Sum(d => d.AssignedHours),
                assignedHours);
            cosmosDbMock
                .Verify(p =>
                    p.GetItemsAsync(
                        It.IsAny<Expression<Func<InternalDroneUtilization, bool>>>(),
                        ownerId),
                    Times.Once);
        }
    }
}
