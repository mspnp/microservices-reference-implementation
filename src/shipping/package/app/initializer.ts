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
            var db = (await MongoClient.connect(connection));
            await db.command({ shardCollection: db.databaseName + '.' + collectionName, key: { tag: "hashed" } });
        }
        catch (ex) {
            if (ex.code != MongoErrors.CommandNotFound && ex.code != 9) {
                console.log(ex);
            }
        }
    }

    private static initAppInsights(cloudRole = "package") {
        appInsights.setup();
        appInsights.defaultClient.context.tags[appInsights.defaultClient.context.keys.cloudRole] = cloudRole;
        appInsights.start();
        console.log('Application Insights started');
    }
}

