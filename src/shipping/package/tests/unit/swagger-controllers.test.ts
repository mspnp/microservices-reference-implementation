// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

const pkg = require('../../package.json');
const supertest = require('supertest');

import { KoaApp } from '../../app/app';

const app = KoaApp.create('debug');

const server = app.listen();

afterAll((done) => {
  server.close(done)
});

describe('SwaggerControllers', () => {
  const request = supertest(server);

  describe('GET /', () => {
    it('<200> should always return with the openAPI specinformation', async () => {
      // Arrange
      // N/A

      // Act
      const res = await request
        .get('/swagger/swagger.json')
        .expect('Content-Type', /json/)
        .expect(200);

      const spec = res.body;

      // Assert
      const expected = ["openapi", "info", "basePath", "schemes", "consumes", "produces", "paths", "definitions", "components", "tags"];
      expect(Object.keys(spec)).toEqual(expect.arrayContaining(expected));
      expect(spec.info.title).toBe(pkg.name);
      expect(spec.info.version).toBe(pkg.version);
      expect(spec.info.description).toBe(pkg.description);
      expect(spec.info.contact).toBe(pkg.author);
    });
  });

});
