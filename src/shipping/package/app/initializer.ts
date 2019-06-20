// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { MongoErrors } from './util/mongo-err'

let appInsights = require('applicationinsights');
var MongoClient = require('mongodb').MongoClient;

export class PackageServiceInitializer
{
    static async initialize(connection: string, collectionName: string, containerName: string) {
        try {
            PackageServiceInitializer.initAppInsights(containerName);
            await PackageServiceInitializer.initMongoDb(connection,
                                                        collectionName);
        }
        catch(ex) {
            console.log(ex);
        }
    }

    private static async initMongoDb(connection: string, collectionName: string) {
        try {
            var db = (await MongoClient.connect(connection)).db();
            await db.command({ shardCollection: db.databaseName + '.' + collectionName, key: { tag: "hashed" } });
        }
        catch (ex) {
            if (ex.code != MongoErrors.CommandNotFound && ex.code != 9) {
                console.log(ex);
            }
        }
    }

    private static initAppInsights(cloudRole = "package") {
        if (!process.env.APPINSIGHTS_INSTRUMENTATIONKEY &&
                process.env.NODE_ENV === 'development') {
            const logger = console;
            process.stderr.write('Skipping app insights setup - in development mode with no ikey set\n');
            appInsights.
                defaultClient = {
                    trackEvent: logger.log.bind(console, 'trackEvent'),
                    trackException: logger.error.bind(console, 'trackException'),
                    trackMetric: logger.log.bind(console, 'trackMetric'),
                };
        } else if (process.env.APPINSIGHTS_INSTRUMENTATIONKEY) {
            appInsights.setup();
            appInsights.defaultClient.context.tags[appInsights.defaultClient.context.keys.cloudRole] = cloudRole;
            process.stdout.write('App insights setup - configuring client\n');
            appInsights.start();
            process.stdout.write('Application Insights started');
        } else {
            throw new Error('No app insights setup. A key must be specified in non-development environments.');
        }
    }
}

