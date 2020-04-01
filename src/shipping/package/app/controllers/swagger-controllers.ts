// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import * as Spec from '../spec/package-swagger';
import { ILogger } from '../util/logging'

export class SwaggerControllers {

    static async getSpec(ctx: any) {
        var logger : ILogger = ctx.state.logger;
        logger.info('Swagger Spec Request %s', "OK");

        ctx.status = 200;
        ctx.body = Spec.PackageServiceSwaggerApi;
    }

}
