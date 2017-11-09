// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Fabrikam.DroneDelivery.DeliveryService.Middlewares
{
    public sealed class GlobalInternalErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ExceptionHandlerOptions _options;
        private readonly Func<object, Task> _clearCacheHeadersDelegate;

        public GlobalInternalErrorHandlerMiddleware(
            RequestDelegate next,
            IOptions<ExceptionHandlerOptions> options)
        {
            _next = next;
            _options = options.Value;
            _clearCacheHeadersDelegate = ClearCacheHeaders;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);

                //TODO: to continue globably handling errs, please add them below within this closure. If not they will be handled as 500 
            }
            //The actual global err handling
            catch (Exception ex)
            {
                // We can't do anything if the response has already started, just abort. (not an internal server error here)
                if (context.Response.HasStarted)
                {
                    throw;
                }

                await HandleException(context, ex, 500);
            }
        }

        private async Task HandleException(HttpContext context, Exception ex, int httpStatusCode)
        {
            PathString originalPath = context.Request.Path;
            if (_options.ExceptionHandlingPath.HasValue)
            {
                context.Request.Path = _options.ExceptionHandlingPath;
            }

            try
            {
                context.Response.Clear();
                var exceptionHandlerFeature = new ExceptionHandlerFeature()
                {
                    Error = ex,
                    Path = originalPath.Value,
                };

                context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
                context.Features.Set<IExceptionHandlerPathFeature>(exceptionHandlerFeature);
                context.Response.StatusCode = httpStatusCode;
                context.Response.OnStarting(_clearCacheHeadersDelegate, context.Response);

                if (_options.ExceptionHandler != null)
                {
                    await _options.ExceptionHandler(context);
                }

                return;
            }
            finally
            {
                context.Request.Path = originalPath;
            }
        }

        private Task ClearCacheHeaders(object state)
        {
            var response = (HttpResponse)state;
            response.Headers[HeaderNames.CacheControl] = "no-cache";
            response.Headers[HeaderNames.Pragma] = "no-cache";
            response.Headers[HeaderNames.Expires] = "-1";
            response.Headers.Remove(HeaderNames.ETag);
            return Task.CompletedTask;
        }
    }
}
