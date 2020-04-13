// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

const Router = require('@koa/router');

import { PackageControllers, HealthzControllers, SwaggerControllers } from './controllers';

export function apiRouter() {

    const router = new Router({
      prefix: '/api'
    });

    router.get('/packages/:packageId', PackageControllers.getById);
    router.put('/packages', PackageControllers.createOrUpdate);
    router.put('/packages/:packageId', PackageControllers.createOrUpdate);
    router.patch('/packages/:packageId', PackageControllers.updateById);

    router.get('/packages/summary/:ownerId', PackageControllers.getSummary);

    return router;
}

export function healthzRouter() {

    const router = new Router({
      prefix: '/healthz'
    });

    router.get('/', HealthzControllers.getReadinessLiveness);

    return router;
}

export function swaggerRouter() {

    const router = new Router({
      prefix: '/swagger'
    });

    router.get('/swagger.json', SwaggerControllers.getSpec);

    return router;
}
