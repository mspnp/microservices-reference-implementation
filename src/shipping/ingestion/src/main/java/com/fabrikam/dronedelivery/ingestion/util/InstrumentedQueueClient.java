// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

package com.fabrikam.dronedelivery.ingestion.util;

import java.util.concurrent.CompletableFuture;

import com.microsoft.azure.servicebus.IMessage;

public interface InstrumentedQueueClient {
	CompletableFuture<Void> sendAsync(IMessage message);
}
