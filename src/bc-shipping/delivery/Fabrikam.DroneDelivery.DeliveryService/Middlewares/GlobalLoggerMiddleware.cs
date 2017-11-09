// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fabrikam.DroneDelivery.DeliveryService.Middlewares
{
    public sealed class GlobalLoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly DiagnosticSource _diagnosticSource;

        public GlobalLoggerMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            DiagnosticSource diagnosticSource)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<GlobalLoggerMiddleware>();
            _diagnosticSource = diagnosticSource;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);

                if (context.Response.StatusCode >= 400 && context.Response.StatusCode < 500)
                {
                    _logger.LogError("An error has occurred: {StatusCode}", context.Response.StatusCode);
                    if (_diagnosticSource.IsEnabled("Microsoft.AspNetCore.Diagnostics.HandledException.Client"))
                    {
                        _diagnosticSource.Write("Microsoft.AspNetCore.Diagnostics.HandledException.Client", new { httpContext = context });
                    }
                }
                else if (context.Response.StatusCode >= 500)
                {
                    var exFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (exFeature != null)
                    {
                        _logger.LogError(exFeature.Error, "An internal handled exception has occurred: {ExceptionMessage}", exFeature.Error.Message);
                        if (_diagnosticSource.IsEnabled("Microsoft.AspNetCore.Diagnostics.HandledException.Server"))
                        {
                            _diagnosticSource.Write("Microsoft.AspNetCore.Diagnostics.HandledException.Server", new { httpContext = context, exception = exFeature.Error });
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                //TODO: consider adding the event id(s) as constants
                //Important: Something went really wrong!!!
                _logger.LogError(ex, "An exception was thrown attempting to execute the global internal server error handler: {ExceptionMessage}", ex.Message);
                if (_diagnosticSource.IsEnabled("Microsoft.AspNetCore.Diagnostics.UnhandledException"))
                {
                    _diagnosticSource.Write("Microsoft.AspNetCore.Diagnostics.UnhandledException", new { httpContext = context, exception = ex });
                }

                // We can't do anything if the response has already started, just abort. (not an internal server error here)
                if (context.Response.HasStarted)
                {
                    //TODO: consider adding all the events as constants
                    _logger.LogWarning("The response has already started, the error handler will not be executed.");
                }

                throw; // Re-throw the original since we couldn't handle it
            }
        }
    }
}