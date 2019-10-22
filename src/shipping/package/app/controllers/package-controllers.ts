// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { Repository, UpsertStatus } from '../models/repository'
import * as apiModels from '../models/api-models'
import { Package, PackageSize } from '../models/package'
import { ILogger } from '../util/logging'
import { MongoErrors } from '../util/mongo-err'

export class PackageControllers {

  constructor(private repository: Repository) {

  }

  mapPackageDbToApi(pkg: Package): apiModels.Package {
    // consider moving this mapping code to data access
    // might also want to consider allowing repository to deal with API types or.. automapping, mapping through convention or config
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

  async getById(ctx: any, next: any) {

    var logger : ILogger = ctx.state.logger;
    var packageId = ctx.params.packageId;
    logger.info('Entering getById, packageId = %s', packageId);

    await next();

    let pkg = await this.repository.findPackage(ctx.params.packageId)

    if (pkg == null) {
      logger.info(`getById: %s not found`, packageId);
      ctx.response.status= 404;
      return;
    }

    ctx.response.status = 200;
    ctx.response.body = this.mapPackageDbToApi(pkg);
  }

  async updateById(ctx: any, next: any) {

    var logger : ILogger = ctx.state.logger;
    var packageId = ctx.params.packageId;
    logger.info('updateById', packageId);

    try {
      await next();

      let apiPkg = <apiModels.Package>ctx.request.body;
      let pkg = this.mapPackageApiToDb(apiPkg, packageId)

      // the update package should take a dictionary of fields instead of package model
      await this.repository.updatePackage(pkg);

      ctx.response.status = 204;
    }
    catch (ex) {
      // Need to handle the case were it's 404 vs invalid data
      ctx.throw(ex.message, 400);
    }

    return;
  }

  // Creates or updates a package using the data provided in the API
  async createOrUpdate(ctx: any, next: any) {

    var logger : ILogger = ctx.state.logger;
    var packageId = ctx.params.packageId;
    logger.info('create', ctx.request.body)

    try {
      await next();

      let apiPkg = <apiModels.Package>ctx.request.body;
      let pkg = this.mapPackageApiToDb(apiPkg, packageId);

      var result = await this.repository.addPackage(pkg);

      switch (result) {
        case UpsertStatus.Created:
          ctx.body = this.mapPackageDbToApi(pkg);
          ctx.response.status = 201;
          break;
        case UpsertStatus.Updated:
          ctx.response.status = 204;
          break;
      }

      return;
    }
    catch (ex) {
      switch (ex.code) {
        case MongoErrors.ShardKeyNotFound:
          logger.error('Missing shard key', ctx.request.body)
          ctx.response.status = 400;
          ctx.response.message = "Missing shard key";
          break;

        case MongoErrors.TooManyRequests:
          logger.error('Too many requests', ctx.request.body)
          ctx.response.status = 429;
          break;

        default:
          ctx.throw(ex.message, 500);
      }
    }
  }

  // Get summary information about packages from a user
  async getSummary(ctx: any, next: any) {

    var logger : ILogger = ctx.state.logger;
    var ownerId = ctx.params.ownerId;
    var year = ctx.params.year;
    var month = ctx.params.month;
    logger.info('retrieve summary %s %d/%d', ownerId)

    await next();

    let utilization = new apiModels.PackageUtilization();
    utilization.totalWeight = 400;

    ctx.body = utilization;
    ctx.response.status = 200;

    return;
  }
}
