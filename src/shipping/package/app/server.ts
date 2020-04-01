// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { KoaApp } from './app';
import { Repository } from './models/repository';
import { Settings } from './util/settings';

export class PackageService {

  static start() {

    const port = process.env.PORT || 80;

    console.log('Package service starting...')

    // Initialize repository with connection string
    Promise.resolve(Repository.initialize(Settings.connectionString()))
      .catch((ex) => {
        console.error("failed to initialize repository - make sure a connectiong string has been configured");
        console.error(ex.message);
        process.exit(1);  // Crash the container
      });

    const app = KoaApp.create(Settings.logLevel());

    // Add package repo to the context
    app.context.packageRepository = new Repository();

    app.listen(port);
    console.log('listening on port %s', port);
  }
}
