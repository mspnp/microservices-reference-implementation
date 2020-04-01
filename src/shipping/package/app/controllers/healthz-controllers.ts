// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { ILogger } from '../util/logging'

export class HealthzControllers {

    static async getReadinessLiveness(ctx: any) {
        var logger : ILogger = ctx.state.logger;
        logger.info('Readiness/Liveness Probe Status: %s', "OK");

        ctx.status = 200;
        ctx.body = {status: 'OK'};
    }

}
