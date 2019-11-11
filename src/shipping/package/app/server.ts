// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

const _ = require('koa-route');
import * as Koa from 'koa';
import * as bodyParser from "koa-bodyparser";

var compress = require('koa-compress');

const fleekCtx = require('fleek-context');
const fleekRouter = require('fleek-router');
const fleekValidator = require('fleek-validator');

const SWAGGER = require('./api.json');

import { PackageControllers } from './controllers';
import { Repository } from './models/repository';
import { Settings } from './util/settings';
import { logger, ILogger } from './util/logging'

export class PackageService {

  static start() {

    console.log('Package service starting...')

    // Initialize repository with connection string
    Promise.resolve(Repository.initialize(Settings.connectionString()))
      .catch((ex) => {
        console.error("failed to initialize repository - make sure a connectiong string has been configured");
        console.error(ex.message);
        process.exit(1);  // Crash the container
      });

    var packageControllers = new PackageControllers(new Repository());

    let app = new Koa();

    // Configure logging
    app.use(logger(Settings.logLevel()));

    // Add simple health check endpoint
    app.use(_.get('/healthz', (ctx) => {
        var logger : ILogger = ctx.state.logger;
        logger.info('Readiness/Liveness Probe Status: %s', "OK");

        ctx.status = 200;
        ctx.body = {status: 'OK'};
    }));

    // Configure global exception handling
    // Use: ctx.throw('Error Message', 500);
    //   in the controller methods to set the status code and exception message
    app.use(async (ctx, next) => {
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

    // configure fleek to handle validation and routing based on api document
    app.use(fleekCtx(SWAGGER));
    app.use(fleekValidator().catch((ctx, next) => {
      if (ctx.fleek.validation.failed) {

        var logger : ILogger = ctx.state.logger;
        if (logger) {
          logger.error(ctx.fleek.validation);
        }

        ctx.body = ctx.fleek.validation;
        ctx.status = 400;
        return;
      }
      return next();
    }));

    let router = new fleekRouter.Router({ controllers: `${__dirname}/controllers` });

    app.use(router.controllers({
      operation: packageControllers
    }));

    console.log('listening on port 80');
    app.listen(80);
  }
}
