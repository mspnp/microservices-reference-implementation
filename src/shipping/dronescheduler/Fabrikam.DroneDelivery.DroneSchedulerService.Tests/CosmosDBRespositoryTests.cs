// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;
using Fabrikam.DroneDelivery.DroneSchedulerService.Services;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Tests
{
    public class CosmosDBRespositoryTests
    {
        private readonly ILogger<CosmosRepository<InternalDroneUtilization>> _loggerDebug;

        private readonly IOptions<CosmosDBRepositoryOptions<InternalDroneUtilization>> _optionsMockObject;
        private readonly Container _containerMockObject;
        private readonly CosmosClient _clientMockObject;
        private readonly ICosmosDBRepositoryMetricsTracker<InternalDroneUtilization> _metricsTrackerMockObject;

        private readonly List<InternalDroneUtilization> _fakeResults;

        public CosmosDBRespositoryTests()
        {
            var servicesBuilder = new ServiceCollection();
            servicesBuilder.AddLogging(logging => logging.AddDebug());
            var services = servicesBuilder.BuildServiceProvider();

            _loggerDebug = services.GetService<
                ILogger<
                    CosmosRepository<
                        InternalDroneUtilization>>>();

            _fakeResults = new List<InternalDroneUtilization> {
                new InternalDroneUtilization {
                    Id = "d0001",
                    PartitionKey = "o00042",
                    OwnerId = "o00042",
                    Month = 6,
                    Year = 2019,
                    TraveledMiles =10d,
                    AssignedHours=1d,
                    DocumentType = typeof(InternalDroneUtilization).Name
                },
                new InternalDroneUtilization {
                    Id = "d0002",
                    PartitionKey = "o00042",
                    OwnerId = "o00042",
                    Month = 6,
                    Year = 2019,
                    TraveledMiles=32d,
                    AssignedHours=2d,
                    DocumentType = typeof(InternalDroneUtilization).Name
                }
            };

            _clientMockObject = Mock.Of<CosmosClient>(c => c.ClientOptions == new CosmosClientOptions());

            var responseMock = new Mock<FeedResponse<InternalDroneUtilization>>();
            responseMock.Setup(r => r.Count).Returns(() => _fakeResults.Count);
            responseMock.Setup(r => r.GetEnumerator()).Returns(() => _fakeResults.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<InternalDroneUtilization>>();
            mockFeedIterator.Setup(i => i.HasMoreResults).Returns(new Queue<bool>(new[] { true, false }).Dequeue);
            mockFeedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(responseMock.Object);

            _containerMockObject =
                Mock.Of<Container>(c =>
                    c.GetItemQueryIterator<InternalDroneUtilization>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())
                        == mockFeedIterator.Object);

            var fakeOptionsValue =
                new CosmosDBRepositoryOptions<InternalDroneUtilization>
                {
                    Container = _containerMockObject
                };

            var optionsMock = new Mock<
                IOptions<
                    CosmosDBRepositoryOptions<
                        InternalDroneUtilization>>>();
            optionsMock
                .Setup(o => o.Value)
                .Returns(fakeOptionsValue);

            _optionsMockObject = optionsMock.Object;

            _metricsTrackerMockObject =
                Mock.Of<ICosmosDBRepositoryMetricsTracker<InternalDroneUtilization>>(
                    t => t.GetQueryMetricsTracker(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<int>(),
                            It.IsAny<int>(),
                            It.IsAny<ConnectionMode>(),
                            It.IsAny<int>())
                        == Mock.Of<ICosmosDBRepositoryQueryMetricsTracker<InternalDroneUtilization>>());
        }

        [Fact]
        public async Task WhenGetItemsAsyncWithPartitionId_ThenClientMakesAQueryWithPartitionId()
        {
            // Arrange
            string ownerId = "o00042";

            var repo = new CosmosRepository<InternalDroneUtilization>(
                _clientMockObject,
                _optionsMockObject,
                _loggerDebug,
                _metricsTrackerMockObject);

            // Act
            var res = await repo.GetItemsAsync(
                new QueryDefinition("SELECT *"),
                ownerId);

            // Assert
            Assert.NotNull(res);
            Assert.Equal(_fakeResults.Count(), res.Count());
            Assert.All(
                res,
                r =>
                {
                    Assert.Equal(ownerId, r.PartitionKey);
                    Assert.Equal(typeof(InternalDroneUtilization).Name, r.DocumentType);
                });

            Mock.Get(_containerMockObject)
                .Verify(c =>
                c.GetItemQueryIterator<InternalDroneUtilization>(
                    It.IsAny<QueryDefinition>(), 
                    null, 
                    It.Is<QueryRequestOptions>(ro => 
                        ro.PartitionKey != null
                        && ro.PartitionKey.ToString().Contains(ownerId))));
        }

        [Fact]
        public async Task WhenGetItemsAsyncWithoutPartitionId_ThenClientMakesAQueryWithoutPartitionIdAndEnablesCrossPartition()
        {
            // Arrange
            string ownerId = "o00042";

            var repo = new CosmosRepository<InternalDroneUtilization>(
                _clientMockObject,
                _optionsMockObject,
                _loggerDebug,
                _metricsTrackerMockObject);

            // Act
            var res = await repo.GetItemsAsync(
                new QueryDefinition("SELECT *"),
                null);

            // Assert
            Assert.NotNull(res);
            Assert.Equal(_fakeResults.Count(), res.Count());
            Assert.All(
                res,
                r =>
                {
                    Assert.Equal(ownerId, r.PartitionKey);
                    Assert.Equal(typeof(InternalDroneUtilization).Name, r.DocumentType);
                });
            Mock.Get(_containerMockObject)
                .Verify(c =>
                c.GetItemQueryIterator<InternalDroneUtilization>(
                    It.IsAny<QueryDefinition>(),
                    null,
                    It.Is<QueryRequestOptions>(ro => ro.PartitionKey == null)));
        }
    }
}
