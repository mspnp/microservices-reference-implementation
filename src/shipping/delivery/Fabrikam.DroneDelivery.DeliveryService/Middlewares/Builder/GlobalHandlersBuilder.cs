// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Builder;

namespace Fabrikam.DroneDelivery.DeliveryService.Middlewares.Builder
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Adds a middleware to the pipeline that will globaly catch exceptions.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<GlobalInternalErrorHandlerMiddleware>();
        }

        /// <summary>
        /// Adds a middleware to the pipeline that will globaly log handled and unhandled exceptions.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGlobalLoggerHandler(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<GlobalLoggerMiddleware>();
        }
    }
}
