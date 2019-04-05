// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import * as winston from 'winston';

export interface ILogger {
    log(level: string, msg: string, meta?: any)
    debug(msg: string, meta?: any)
    info(msg: string, meta?: any)
    warn(msg: string, meta?: any)
    error(msg: string, meta?: any)
}
export function logger(level: string) {

    winston.configure({
        level: level,
        transports: [
            new (winston.transports.Console)({
                level: level,
                colorize: true,
                timestamp: true
            })]
        });
    return async function(ctx: any, next: any) {
        ctx.state.logger = new WinstonLogger();
        await next();
    }
}
class WinstonLogger {
    constructor() {}
    log(level: string, msg: string, payload?: any) {
        var meta : any = {};
        if (payload) {
            winston.log(level, msg, payload);
        }
        else
        {
            winston.log(level, msg);
        }
    }

    info(msg: string, payload?: any) {
        this.log('info', msg, payload);
    }
    debug(msg: string, payload?: any) {
        this.log('debug', msg, payload);
    }
    warn(msg: string, payload?: any) {
        this.log('warn', msg, payload);
    }
    error(msg: string, payload?: any) {
        this.log('error', msg, payload);
    }
}
