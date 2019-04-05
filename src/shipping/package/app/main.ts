// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { PackageServiceInitializer } from './initializer'
import { PackageService } from './server';
import { Settings } from './util/settings';

PackageServiceInitializer.initialize(Settings.connectionString(), Settings.collectionName(), Settings.containerName())
    .then(_ => {
        PackageService.start();
    });
