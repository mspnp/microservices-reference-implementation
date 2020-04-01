// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { UpsertStatus } from '../models/repository'
import * as apiModels from '../models/api-models'
import { Package, PackageSize } from '../models/package'
import { ILogger } from '../util/logging'
import { MongoErrors } from '../util/mongo-err'

export class PackageControllers {
  /**
   * @swagger
   *
   * definitions:
   *   Package:
   *     type: object
   *     properties:
   *       id:
   *         type: string
   *       size:
   *         type: string
   *         enum:
   *         - small
   *         - medium
   *         - large
   *       weight:
   *         type: number
   *       tag:
   *         type: string
   *   PackageUtilization:
   *     type: object
   *     properties:
   *       totalWeight:
   *         type: number
   *   Error:
   *     type: object
   *     properties:
   *       code:
   *         type: number
   *       message:
   *         type: string
   */

  /**
   * @swagger
   * /packages/{packageId}:
   *   get:
   *     summary: Get information about a specific package from the service
   *     description: Returns package by id
   *     operationid: getById
   *     parameters:
   *       - name: packageId
   *         description: ID of package to return
   *         in: path
   *         required: true
   *         type: string
   *     responses:
   *       200:
   *         description: successful operation
   *         schema:
   *           $ref: '#/definitions/Package'
   *       400:
   *         description: Invalid ID supplied
   *       404:
   *         description: Package not found
   */
  static async getById(ctx: any) {

    var logger : ILogger = ctx.state.logger;
    var packageId = ctx.params.packageId;
    logger.info('Entering getById, packageId = %s', packageId);

    let pkg = await ctx.packageRepository.findPackage(ctx.params.packageId)

    if (pkg == null) {
      logger.info(`getById: %s not found`, packageId);
      ctx.response.status= 404;
      return;
    }

    ctx.response.status = 200;
    ctx.response.body = ctx.packageRepository.mapPackageDbToApi(pkg);
  }

  /**
   * @swagger
   * /packages/{packageId}:
   *   patch:
   *     summary: Update an existing package
   *     description: Update Package by ID
   *     operationid: updateById
   *     parameters:
   *       - name: packageId
   *         description: ID of package to patch
   *         in: path
   *         required: true
   *         type: string
   *     responses:
   *       204:
   *         description: successful operation
   *       400:
   *         description: invalid id supplied
   *       404:
   *         description: package not found
   *       405:
   *         description: Validation exception
   */
  static async updateById(ctx: any) {

    var logger : ILogger = ctx.state.logger;
    var packageId = ctx.params.packageId;
    logger.info('updateById', packageId);

    try {
      let apiPkg = <apiModels.Package>ctx.request.body;
      let pkg = ctx.packageRepository.mapPackageApiToDb(apiPkg, packageId)

      // the update package should take a dictionary of fields instead of package model
      await ctx.packageRepository.updatePackage(pkg);

      ctx.response.status = 204;
    }
    catch (ex) {
     // Need to handle the case were it's 404 vs invalid data
     ctx.throw(400, ex.message);
    }

    return;
  }

  /**
   * @swagger
   * /packages/{packageId}:
   *   put:
   *     summary: Create or update a package
   *     description: Creates or updates a package using the data provided in the API
   *     operationid: createOrUpdate
   *     parameters:
   *       - name: packageId
   *         description: ID of package to patch
   *         in: path
   *         required: true
   *         type: string
   *     responses:
   *       201:
   *         description: Created new package
   *         schema:
   *           $ref: '#/definitions/Package'
   *       204:
   *         description: Updated existing package
   */
  static async createOrUpdate(ctx: any) {

    var logger : ILogger = ctx.state.logger;
    var packageId = ctx.params.packageId;
    logger.info('create', ctx.request.body)

    try {
      let apiPkg = <apiModels.Package>ctx.request.body;
      let pkg = ctx.packageRepository.mapPackageApiToDb(apiPkg, packageId);

      var result = await ctx.packageRepository.addPackage(pkg);

      switch (result) {
        case UpsertStatus.Created:
          ctx.body = ctx.packageRepository.mapPackageDbToApi(pkg);
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
       {
         ctx.throw(500, ex.message);
       }
     }
    }
  }

  /**
   * @swagger
   * /packages/summary/{ownerId}:
   *   get:
   *     summary: Get summary information about packages from a user
   *     description: Get summary information about packages from a user
   *     operationid: getSummary
   *     parameters:
   *       - name: ownerId
   *         description: ID of the package's owner
   *         in: path
   *         required: true
   *         type: string
   *       - name: year
   *         description: Year of the summary requested
   *         in: query
   *         required: true
   *         type: integer
   *       - name: month
   *         description: Month of the summary requested
   *         in: path
   *         required: true
   *         type: integer
   *     responses:
   *       201:
   *         description: successful operation
   *         schema:
   *           $ref: '#/definitions/PackageUtilization'
   *       400:
   *         description: Invalid ID, year, or month supplied
   */
  static async getSummary(ctx: any) {

    var logger : ILogger = ctx.state.logger;
    var ownerId = ctx.params.ownerId;
    var year = ctx.params.year;
    var month = ctx.params.month;
    logger.info('retrieve summary %s %d/%d', ownerId)

    let utilization = new apiModels.PackageUtilization();
    utilization.totalWeight = 400;

    ctx.body = utilization;
    ctx.response.status = 200;

    return;
  }
}
