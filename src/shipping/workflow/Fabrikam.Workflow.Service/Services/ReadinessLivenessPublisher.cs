// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Fabrikam.Workflow.Service.Services
{
    public class ReadinessLivenessPublisher : IHealthCheckPublisher
    {
        public const string FilePath = "healthz";

        private readonly ILogger _logger;

        public ReadinessLivenessPublisher(ILogger<ReadinessLivenessPublisher> logger)
        {
            this._logger = logger;
        }

        public Task PublishAsync(HealthReport report,
                CancellationToken cancellationToken)
        {
            switch (report.Status)
            {
                case HealthStatus.Healthy:
                {
                    this._logger.LogInformation(
                            "{Timestamp} Readiness/Liveness Probe Status: {Result}",
                            DateTime.UtcNow,
                            report.Status);

                    CreateOrUpdateHealthz();

                    break;
                }

                case HealthStatus.Degraded:
                {
                    this._logger.LogWarning(
                            "{Timestamp} Readiness/Liveness Probe Status: {Result}",
                            DateTime.UtcNow,
                            report.Status);

                    break;
                }

                case HealthStatus.Unhealthy:
                {
                    this._logger.LogError(
                            "{Timestamp} Readiness Probe/Liveness Status: {Result}",
                            DateTime.UtcNow,
                            report.Status);

                    break;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        private static void CreateOrUpdateHealthz()
        {
            if (File.Exists(FilePath))
            {
                File.SetLastWriteTimeUtc(FilePath, DateTime.UtcNow);
            }
            else
            {
                File.AppendText(FilePath).Close();
            }
        }
   }
}
