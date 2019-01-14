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

        public RequestProcessor(ILogger<RequestProcessor> logger, IPackageServiceCaller packageServiceCaller)
        {
            _logger = logger;
            _packageServiceCaller = packageServiceCaller;
        }

        public async Task<bool> ProcessDeliveryRequestAsync(Delivery deliveryRequest, IReadOnlyDictionary<string, object> properties)
        {
            _logger.LogInformation("Processing delivery request {deliveryId}", deliveryRequest.DeliveryId);

            try
            {
                var packageGen = await CreatePackageAsync(deliveryRequest.PackageInfo).ConfigureAwait(false);
                if (packageGen != null)
                {
                    _logger.LogInformation("Generated package {packageId} for delivery {deliveryId}", packageGen.Id, deliveryRequest.DeliveryId);

                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing delivery request {deliveryId}", deliveryRequest.DeliveryId);
            }

            return false;
        }

        private async Task<PackageGen> CreatePackageAsync(PackageInfo packageInfo)
        {
            var packageGen = await _packageServiceCaller.CreatePackageAsync(packageInfo);
            return packageGen;
        }
    }
}
