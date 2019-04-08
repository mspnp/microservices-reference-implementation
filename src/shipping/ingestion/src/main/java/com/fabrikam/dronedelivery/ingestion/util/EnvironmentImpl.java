// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

package com.fabrikam.dronedelivery.ingestion.util;

import org.springframework.stereotype.Component;

@Component
public class EnvironmentImpl implements Environment  {
    public String getenv(String name) {
        return System.getenv(name);
    }
}
