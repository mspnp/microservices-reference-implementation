// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { Package, PackageSize } from './package'
import * as apiModels from './api-models'
import { Settings } from '../util/settings';
import * as Logger from '../util/logging';
import { MongoErrors } from '../util/mongo-err';

var MongoClient = require('mongodb').MongoClient;

export enum UpsertStatus {
    Created = 1,
    Updated = 2,
}

export class Repository
{
  static readonly collectionName = Settings.collectionName();
  private static db:any;


  static async initialize(connection: string) {
      Repository.db = (await MongoClient.connect(connection)).db();
  }

  private collection() {
      return Repository.db.collection(Repository.collectionName);
  }

  async addPackage(p: Package) : Promise<UpsertStatus> {
    try {
        var collection = this.collection();
        var result = await collection.insertOne(p);
        return UpsertStatus.Created;
    } catch (ex) {
        if (ex.code == MongoErrors.DuplicateKey) {
            var result = await collection.updateOne({"_id": p._id, "tag": p.tag}, {$set: p}, {upsert:true});
            return result.upsertedCount > 0 ? UpsertStatus.Created : UpsertStatus.Updated;
        }
        else {
            throw ex;
        }
    }
  }

  async updatePackage(p: Package) {
      await this.collection().update({_id:p._id}, p);
  }

  async findPackage(id: string) : Promise<Package> {
      var collection = this.collection();
      return await collection.findOne({_id: id });  // Returns null if not found
  }

  mapPackageDbToApi(pkg: Package): apiModels.Package {
    // consider allowing repository to deal with API types or.. automapping, mapping through convention or config
    // we could simpley add/remove/change the model in the api vs database
    let apiPkg = new apiModels.Package();
    apiPkg.id = pkg._id;
    apiPkg.size = pkg.size ? pkg.size.toString() : null;
    apiPkg.tag = pkg.tag;
    apiPkg.weight = pkg.weight;
    return apiPkg;
  }

  mapPackageApiToDb(apiPkg: apiModels.Package, id?: string): Package {
    let pkg = new Package(id);
    pkg.size = <PackageSize>apiPkg.size;
    pkg.weight = apiPkg.weight;
    pkg.tag = apiPkg.tag;
    return pkg;
  }

}

