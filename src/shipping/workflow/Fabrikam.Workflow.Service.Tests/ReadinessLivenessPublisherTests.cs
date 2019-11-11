// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Fabrikam.Workflow.Service.Services;
using Moq;
using Xunit;

namespace Fabrikam.Workflow.Service.Tests
{
    public class ReadinessLivenessPublisherTests
    {
        private const int DelayCompletionMs = 1000;

        private readonly ReadinessLivenessPublisher _publisher;

        public ReadinessLivenessPublisherTests()
        {
            var servicesBuilder = new ServiceCollection();
            servicesBuilder.AddLogging(logging => logging.AddDebug());
            var services = servicesBuilder.BuildServiceProvider();

            _publisher =
                new ReadinessLivenessPublisher(
                    services.GetService<ILogger<ReadinessLivenessPublisher>>());
        }

        [Fact]
        public async Task WhenPublishingAndReportIsHealthy_FileExists()
        {
            // Arrange
            var healthReportEntries = new Dictionary<string, HealthReportEntry>()
            {
                {"healthy", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null) }
            };

            // Act
            await _publisher.PublishAsync(
                    new HealthReport(healthReportEntries, TimeSpan.MinValue),
                    new CancellationTokenSource().Token);

            // Arrange
            Assert.True(File.Exists(ReadinessLivenessPublisher.FilePath));
        }

        [Fact]
        public async Task WhenPublishingAndReportIsUnhealthy_FileDateTimeIsNotModified()
        {
            // Arrange
            var healthReportEntries = new Dictionary<string, HealthReportEntry>()
            {
                {"healthy", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null) }
            };

            await _publisher.PublishAsync(
                    new HealthReport(
                        healthReportEntries,
                        TimeSpan.MinValue),
                    new CancellationTokenSource().Token);

            healthReportEntries.Add(
                    "unhealthy",
                    new HealthReportEntry(
                        HealthStatus.Unhealthy,
                        null,TimeSpan.MinValue, null, null));

            // Act
            DateTime healthyWriteTime = File.GetLastWriteTime(ReadinessLivenessPublisher.FilePath);
            await _publisher.PublishAsync(
                    new HealthReport(healthReportEntries, TimeSpan.MinValue),
                    new CancellationTokenSource().Token);

            // Arrange
            Assert.True(File.Exists(ReadinessLivenessPublisher.FilePath));
            Assert.Equal(healthyWriteTime, File.GetLastWriteTime(ReadinessLivenessPublisher.FilePath));
        }

        [Fact(Timeout = DelayCompletionMs * 3)]
        public async Task WhenPublishingAndReportIsHealthyTwice_FileDateTimeIsModified()
        {
            // Arrange
            Func<Task> emulatePeriodicHealthCheckAsync =
                () => Task.Delay(DelayCompletionMs);

            var healthReportEntries = new Dictionary<string, HealthReportEntry>()
            {
                {"healthy", new HealthReportEntry(HealthStatus.Healthy, null,TimeSpan.MinValue, null, null) }
            };

            // Act
            await _publisher.PublishAsync(
                    new HealthReport(
                        healthReportEntries,
                        TimeSpan.MinValue),
                    new CancellationTokenSource().Token);

            DateTime firstTimehealthyWriteTime = File.GetLastWriteTime(ReadinessLivenessPublisher.FilePath);

            await emulatePeriodicHealthCheckAsync();

            await _publisher.PublishAsync(
                    new HealthReport(healthReportEntries, TimeSpan.MinValue),
                    new CancellationTokenSource().Token);

            DateTime sencondTimehealthyWriteTime = File.GetLastWriteTime(ReadinessLivenessPublisher.FilePath);

            // Arrange
            Assert.True(firstTimehealthyWriteTime < sencondTimehealthyWriteTime);
        }
    }
}
