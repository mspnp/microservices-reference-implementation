// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

const supertest = require('supertest');

import * as apiModels from '../../app/models/api-models'
import { Package } from '../../app/models/package'

import { KoaApp } from '../../app/app';

const app = KoaApp.create('debug');

// Add mocks to the context
const mockFindPackage = jest.fn(id =>  (id === "507f1f77bcf86cd799439011") ? new Package(id) : null);
const mockMapPackageDbToApi = jest.fn(pkg => {
  if (pkg == null) return null;

  var pkgApi = new apiModels.Package();
  pkgApi.id = "507f1f77bcf86cd799439011";
  pkgApi.size = "small";
  pkgApi.weight = 1;
  pkgApi.tag = "test";

  return pkgApi;
});

const mockMapPackageApiToDb = jest.fn((pkg, id) => new Package(id));
const mockAddPackage = jest.fn(pkg => {
  switch (pkg._id) {
    case "42":
      return 2;
    case "43":
      throw new Error("mock error");
    default:
      return 1;
  }
});

const mockUpdatePackage = jest.fn(pkg => {return;});
app.context.packageRepository = {
  findPackage: mockFindPackage,
  mapPackageDbToApi: mockMapPackageDbToApi,
  mapPackageApiToDb: mockMapPackageApiToDb,
  addPackage: mockAddPackage,
  updatePackage: mockUpdatePackage
};

const server = app.listen();

afterAll((done) => {
  server.close(done)
});

describe('PackageControllers', () => {
  const request = supertest(server);

  describe('GET /', () => {
    it('<404> should always return when the package id does not exist', async () => {
      //Arrange
      const ramdonId = "507f1f77bcf86cd799439012";

      // Act
      const res = await request
        .get('/api/packages/' + ramdonId);
      // Assert
      expect(res.status).toBe(404);
    });
  });

  describe('GET /', () => {
    it('<200> should always return with the package information', async () => {
      //Arrange
      const ramdonId = "507f1f77bcf86cd799439011";
      const expected = ['id', 'size', 'weight', 'tag'];

      // Act
      const res = await request
        .get('/api/packages/' + ramdonId)
        .expect('Content-Type', /json/)
        .expect(200);
      const pkg = res.body;

      // Assert
      expect(Object.keys(pkg)).toEqual(expect.arrayContaining(expected));
      expect(pkg.id).toBe("507f1f77bcf86cd799439011");
      expect(pkg.size).toBe("small");
      expect(pkg.weight).toBe(1);
      expect(pkg.tag).toBe("test");
    });
  });

  describe('PUT /', () => {
    it('<204> should always return if exists', async () => {
      //Arrange
      const id = "42";

      // Act
      const res = await request
        .put('/api/packages/' + id);

      // Assert
      expect(res.status).toBe(204);

    });
  });

  describe('PUT /', () => {
    it('<201> should always return if not exist and autogenerate id', async () => {
      //Arrange
      const id = "";

      // Act
      const res = await request
        .put('/api/packages/' + id);

      // Assert
      expect(res.status).toBe(201);

    });
  });

  describe('PATCH /', () => {
    it('<204> should always return if updated', async () => {
      //Arrange
      const id = "42";

      // Act
      const res = await request
        .patch('/api/packages/' + id);

      // Assert
      expect(res.status).toBe(204);

    });
  });

  describe('PUT /', () => {
    it('<500> should return 500 if something went really wrong', async () => {
      //Arrange
      const id = "43";

      // Act
      const res = await request
        .put('/api/packages/' + id);

      // Assert
      expect(res.status).toBe(500);

    });
  });

  describe('GET /', () => {
    it('<200> should always return with the package utilization information', async () => {
      //Arrange
      const ownerId = "42";
      const expected = ['totalWeight'];

      // Act
      const res = await request
        .get('/api/packages/summary/' + ownerId)
        .expect('Content-Type', /json/)
        .expect(200);
      const pkg = res.body;

      // Assert
      expect(Object.keys(pkg)).toEqual(expect.arrayContaining(expected));
      expect(pkg.totalWeight).toBe(400);
    });
  });

});
