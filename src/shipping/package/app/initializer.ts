// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { MongoErrors } from './util/mongo-err'

var MongoClient = require('mongodb').MongoClient;

export class PackageServiceInitializer
{
    static async initialize(connection: string, collectionName: string) {
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
}

