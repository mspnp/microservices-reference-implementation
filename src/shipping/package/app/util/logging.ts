// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { createLogger, format, transports }  from 'winston';

const { combine, timestamp, errors, printf, splat } = format;
const defaultFormat = combine(
    timestamp(),
    splat(),
    errors(),
    printf(
        ({
            timestamp,
            level,
            message,
            ...rest
        }) => {
            let restString = JSON.stringify(rest, undefined, 2);
            restString = restString === '{}' ? '' : restString;

            return `[${timestamp}] ${level} - ${message} ${restString}`;
        },
    ),
);

export interface ILogger {
    log(level: string, msg: string, meta?: any)
    debug(msg: string, meta?: any)
    info(msg: string, meta?: any)
    warn(msg: string, meta?: any)
    error(msg: string, meta?: any)
}

export function logger(level: string) {
    const logger = createLogger({
        level: level,
        transports: [
            new transports.Console({ level: level})
        ],
        format: defaultFormat
    });

    logger.info('winston logger created');

    return async function(ctx: any, next: any) {
        ctx.state.logger = logger;
        await next();
    }
}
