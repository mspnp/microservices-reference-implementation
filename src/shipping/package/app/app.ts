// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import Koa from 'koa';
import bodyParser from "koa-bodyparser";

var compress = require('koa-compress');

import { apiRouter, healthzRouter, swaggerRouter } from './routes';
import { logger, ILogger } from './util/logging'

export class KoaApp {

  static create(logLevel: string) : Koa {

    const app = new Koa();

    // Configure logging
    app.use(logger(logLevel));

    // Configure global exception handling
    // Use: ctx.throw('Error Message', 500);
    //   in the controller methods to set the status code and exception message
    app.use(async (ctx : any, next : any) => {
      try {
        await next();
      } catch (ex) {

        var logger : ILogger = ctx.state.logger;
        if (logger) {
          logger.error(ex.message);
        }

        ctx.status = ex.status || 500;
        // consider api specific codes and localized messages as opposed to internal codes
        ctx.body = {
          level: "error",
          code: ex.code,
          message: ex.message
        }
        ctx.app.emit('error', ex, ctx);
      }
    });

    // add compression and body parser to the pipeline
    app.use(compress());
    app.use(bodyParser());

    const packageServiceRouter = apiRouter();
    app.use(packageServiceRouter.routes());
    app.use(packageServiceRouter.allowedMethods());

    const healthChecksRouter = healthzRouter();
    app.use(healthChecksRouter.routes());
    app.use(healthChecksRouter.allowedMethods());

    const swaggerSpecRouter = swaggerRouter();
    app.use(swaggerSpecRouter.routes());
    app.use(swaggerSpecRouter.allowedMethods());

    return app;
  }
}
