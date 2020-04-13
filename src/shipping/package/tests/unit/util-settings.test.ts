// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

const supertest = require('supertest');

import { Settings } from '../../app/util/settings';

describe('Util Settings', () => {

  describe('Settings default values', () => {
    it('settings should return fake values', async () => {
      // Arrange
      // N/A

      // Act
      const colNameReceived = Settings.collectionName();
      const connStrReceived = Settings.connectionString();
      const containerNameReceived = Settings.containerName();
      const logLevelReceived = Settings.logLevel();

      // Assert
      expect(colNameReceived).not.toBeNull();
      expect(connStrReceived).not.toBeNull();
      expect(containerNameReceived).not.toBeNull();
      expect(logLevelReceived).not.toBeNull();
  });

  });  describe('Settings values', () => {
    it('settings should return fake values', async () => {
      // Arrange
      const colNameExpected = "test-col";
      process.env["COLLECTION_NAME"] = colNameExpected;
      const connStrExpected = "test-connstr";
      process.env["CONNECTION_STRING"] = connStrExpected;
      const containerNameExpected = "test-container";
      process.env["CONTAINER_NAME"] = containerNameExpected;
      const logLevelExpected = "test-loglvl";
      process.env["LOG_LEVEL"] = logLevelExpected;

      // Act
      const colNameReceived = Settings.collectionName();
      const connStrReceived = Settings.connectionString();
      const containerNameReceived = Settings.containerName();
      const logLevelReceived = Settings.logLevel();

      // Assert
      expect(colNameReceived).toBe(colNameExpected);
      expect(connStrReceived).toBe(connStrExpected);
      expect(containerNameReceived).toBe(containerNameExpected);
      expect(logLevelReceived).toBe(logLevelExpected);
    });
  });

});
