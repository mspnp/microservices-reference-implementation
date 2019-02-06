// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import process = require("process")

export class Settings {
  static collectionName() : string {
    return process.env["COLLECTION_NAME"]
  }

  static connectionString() : string {
    return process.env["CONNECTION_STRING"]
  }

  static containerName() : string {
    return process.env["CONTAINER_NAME"]
  }

  static logLevel() : string {
    return process.env["LOG_LEVEL"] || 'debug'
  }
}
