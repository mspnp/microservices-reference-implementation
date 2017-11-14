// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { PackageService } from './server';
import { PackageServiceInitializer } from './initializer'
import { Settings } from './util/settings';

PackageServiceInitializer.initialize(Settings.connectionString(), Settings.collectionName())
    .then(_ => {
        PackageService.start();
    });
