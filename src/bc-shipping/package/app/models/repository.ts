// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { Package } from "./package"
import { Settings } from '../util/settings';
import * as Logger from '../util/logging';      

var MongoClient = require('mongodb').MongoClient;

export enum UpsertStatus {
    Created = 1,
    Updated = 2,
}

export class Repository
{
  static readonly collectionName = Settings.collectionName();
  private static db;


  static async initialize(connection: string) {
      Repository.db = await MongoClient.connect(connection);
  }

  private collection() {
      return Repository.db.collection(Repository.collectionName);
  }

  async addPackage(p: Package) {

      //this.logger.info('Repository.addPackage', p);

      var collection = this.collection();
      await collection.insertOne(p);
  }
  
  async upsertPackage(p: Package) : Promise<UpsertStatus> {
    var collection = this.collection();
    var result = await collection.updateOne({"_id": p._id, "tag": p.tag}, {$set: p}, {upsert:true});

    if (result.upsertedCount > 0) {
        return UpsertStatus.Created;
    }

    return UpsertStatus.Updated;
  }

  async updatePackage(p: Package) {
      await this.collection().update({_id:p._id}, p);
  }

  async findPackage(id: string) : Promise<Package> {
      var collection = this.collection();
      return await collection.findOne({_id: id });  // Returns null if not found
  }
}

