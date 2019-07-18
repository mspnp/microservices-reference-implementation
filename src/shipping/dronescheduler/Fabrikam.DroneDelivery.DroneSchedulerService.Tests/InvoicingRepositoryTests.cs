// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.FeatureManagement;
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

            var featureToggleMock = new Mock<IFeatureManager>();
            featureToggleMock.Setup(fm => fm.IsEnabled(It.IsAny<string>())).Returns(true);

            var repo = new InvoicingRepository(cosmosDbMock.Object, featureToggleMock.Object);

            // Act
            var result = await repo.GetAggreatedInvoincingDataAsync(ownerId, year, month);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0d, result.Item1);
            Assert.Equal(0d, result.Item2);
            cosmosDbMock
                .Verify(p =>
                    p.GetItemsAsync(
                        It.IsAny<QueryDefinition>(),
                        ownerId),
                    Times.Once);
        }

        [Fact]
        public async Task WhenGetAggregatedInvoincingDataAndFeatureForPartitionKeyIsDisabled_ThenInvokesDroneUtilizationRepositoryWithoutPartitionKey()
        {
            // Arrange
            string ownerId = "o00042";
            int year = 2019;
            int month = 6;

            var cosmosDbMock = new Mock<ICosmosRepository<InternalDroneUtilization>>();

            var featureToggleMock = new Mock<IFeatureManager>();
            featureToggleMock.Setup(fm => fm.IsEnabled(It.IsAny<string>())).Returns(false);

            var repo = new InvoicingRepository(cosmosDbMock.Object, featureToggleMock.Object);

            // Act
            var result = await repo.GetAggreatedInvoincingDataAsync(ownerId, year, month);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0d, result.Item1);
            Assert.Equal(0d, result.Item2);
            cosmosDbMock
                .Verify(p =>
                    p.GetItemsAsync(
                        It.IsAny<QueryDefinition>(),
                        null),
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
                        It.IsAny<QueryDefinition>(),
                        ownerId))
                    .ReturnsAsync(invoicingData.AsEnumerable());

            var featureToggleMock = new Mock<IFeatureManager>();
            featureToggleMock.Setup(fm => fm.IsEnabled(It.IsAny<string>())).Returns(true);

            var repo = new InvoicingRepository(cosmosDbMock.Object, featureToggleMock.Object);

            // Act
            var (traveledMiles, assignedHours) =
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
                        It.IsAny<QueryDefinition>(),
                        ownerId),
                    Times.Once);
        }
    }
}
