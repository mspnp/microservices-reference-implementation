// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Fabrikam.DroneDelivery.DroneSchedulerService.Models;
using Fabrikam.DroneDelivery.DroneSchedulerService.Services;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Tests
{
    public class DroneDeliveriesUtilizationIntegrationTests :
        IClassFixture<CustomWebApplicationFactory>,
        IDisposable
    {
        private const string RequestUri = "localhost/api/dronedeliveries/utilization";
        private const string ExpectedContentType = "application/json; charset=utf-8";

        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        private readonly List<InternalDroneUtilization> _fakeResults;

        public DroneDeliveriesUtilizationIntegrationTests(
            CustomWebApplicationFactory factory)
        {
            _fakeResults = new List<InternalDroneUtilization>();

            var responseMock = new Mock<FeedResponse<InternalDroneUtilization>>();
            responseMock.Setup(r => r.Count).Returns(() => _fakeResults.Count);
            responseMock.Setup(r => r.GetEnumerator()).Returns(() => _fakeResults.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<InternalDroneUtilization>>();
            mockFeedIterator.Setup(i => i.HasMoreResults).Returns(new Queue<bool>(new[] { true, false }).Dequeue);
            mockFeedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(responseMock.Object);

            var _containerMockObject = Mock.Of<Container>();
            _containerMockObject =
                Mock.Of<Container>(c =>
                    c.GetItemQueryIterator<InternalDroneUtilization>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())
                        == mockFeedIterator.Object);

            var configOptMock = new Mock<IConfigureOptions<CosmosDBRepositoryOptions<InternalDroneUtilization>>>();
            configOptMock
                .Setup(c => c.Configure(
                    It.IsAny<CosmosDBRepositoryOptions<InternalDroneUtilization>>()))
                .Callback<CosmosDBRepositoryOptions<InternalDroneUtilization>>(
                    o => o.Container = _containerMockObject);

            var configOptMockObject = configOptMock.Object;

            var clientMockObject = Mock.Of<CosmosClient>(c => c.ClientOptions == new CosmosClientOptions());

            _factory = factory;
            _client = factory.WithWebHostBuilder(b =>
                b.ConfigureTestServices(s =>
                {
                    s.ConfigureOptions(configOptMockObject);
                    s.AddSingleton(clientMockObject);
                    s.AddSingleton(
                        Mock.Of<ICosmosDBRepositoryMetricsTracker<InternalDroneUtilization>>(
                            t => t.GetQueryMetricsTracker(
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<int>(),
                                    It.IsAny<int>(),
                                    It.IsAny<ConnectionMode>(),
                                    It.IsAny<int>())
                                == Mock.Of<ICosmosDBRepositoryQueryMetricsTracker<InternalDroneUtilization>>()));
                }))
                .CreateClient();
        }

        [Fact]
        public async Task GetInvoicingWithoutQueryParams_ThenResponseBadRequestStatusCode()
        {
            // Arrange
            var droneUtilizationUriWithoutParams = new UriBuilder(RequestUri);

            // Act
            var response = await _client.GetAsync(droneUtilizationUriWithoutParams.ToString());

            // Assert
            Assert.NotNull(response);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetInvoicingMonthlyBasisWithValidCriteria_ThenResponseWithSuccessStatusCode()
        {
            // Arrange
            string ownerId = "o00042";
            int year = 2019, month = 6;

            _fakeResults.Add(
                new InternalDroneUtilization
                {
                    Id = "d0001",
                    PartitionKey = "o00042",
                    OwnerId = "o00042",
                    Month = 6,
                    Year = 2019,
                    TraveledMiles = 10d,
                    AssignedHours = 1d,
                    DocumentType = typeof(InternalDroneUtilization).Name
                });

            var droneUtilizationUriWithParams = new UriBuilder(RequestUri);
            droneUtilizationUriWithParams.Query =
                $"ownerId={ownerId}&year={year}&month={month}";

            // Act
            var response = await _client.GetAsync(droneUtilizationUriWithParams.ToString());

            // Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal(ExpectedContentType,
                response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task GetInvoicingForNotExistentData_ThenResponseWithNotFoundStatusCode()
        {
            // Arrange
            string ownerId = "o00042";
            var minValidDateTime = DateTime.MinValue;
            int year = minValidDateTime.Year, month = minValidDateTime.Month;

            var droneUtilizationUriWithParams = new UriBuilder(RequestUri);
            droneUtilizationUriWithParams.Query =
                $"ownerId={ownerId}&year={year}&month={month}";

            // Act
            var response = await _client.GetAsync(droneUtilizationUriWithParams.ToString());

            // Assert
            Assert.NotNull(response);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }
}