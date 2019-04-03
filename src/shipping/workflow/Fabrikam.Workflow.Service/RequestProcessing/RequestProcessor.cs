// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fabrikam.Workflow.Service.Models;
using Fabrikam.Workflow.Service.Services;

namespace Fabrikam.Workflow.Service.RequestProcessing
{
    public class RequestProcessor : IRequestProcessor
    {
        private readonly ILogger<RequestProcessor> _logger;
        private readonly IPackageServiceCaller _packageServiceCaller;
        private readonly IDroneSchedulerServiceCaller _droneSchedulerServiceCaller;
        private readonly IDeliveryServiceCaller _deliveryServiceCaller;

        public RequestProcessor(
            ILogger<RequestProcessor> logger,
            IPackageServiceCaller packageServiceCaller,
            IDroneSchedulerServiceCaller droneSchedulerServiceCaller,
            IDeliveryServiceCaller deliveryServiceCaller)
        {
            _logger = logger;
            _packageServiceCaller = packageServiceCaller;
            _droneSchedulerServiceCaller = droneSchedulerServiceCaller;
            _deliveryServiceCaller = deliveryServiceCaller;
        }

        public async Task<bool> ProcessDeliveryRequestAsync(Delivery deliveryRequest, IReadOnlyDictionary<string, object> properties)
        {
            _logger.LogInformation("Processing delivery request {deliveryId}", deliveryRequest.DeliveryId);

            try
            {
                var packageGen = await _packageServiceCaller.UpsertPackageAsync(deliveryRequest.PackageInfo).ConfigureAwait(false);
                if (packageGen != null)
                {
                    _logger.LogInformation("Generated package {packageId} for delivery {deliveryId}", packageGen.Id, deliveryRequest.DeliveryId);

                    var droneId = await _droneSchedulerServiceCaller.GetDroneIdAsync(deliveryRequest).ConfigureAwait(false);
                    if (droneId != null)
                    {
                        _logger.LogInformation("Assigned drone {droneId} for delivery {deliveryId}", droneId, deliveryRequest.DeliveryId);

                        var deliverySchedule = await _deliveryServiceCaller.ScheduleDeliveryAsync(deliveryRequest, droneId);
                        if (deliverySchedule != null)
                        {
                            _logger.LogInformation("Completed delivery {deliveryId}", deliveryRequest.DeliveryId);
                            return true;
                        }
                        else
                        {
                            _logger.LogError("Failed delivery for request {deliveryId}", deliveryRequest.DeliveryId);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing delivery request {deliveryId}", deliveryRequest.DeliveryId);
            }

            return false;
        }
    }
}
