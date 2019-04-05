// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

package com.fabrikam.dronedelivery.ingestion.util;

import java.util.concurrent.CompletableFuture;
import java.util.function.Function;

import com.microsoft.azure.servicebus.IMessage;

public interface ServiceBusTracing {

	public CompletableFuture<Void> trackAndCorrelateServiceBusDependency(
		String endpoint, 
		String queueName,
		IMessage message,
		Function<IMessage, CompletableFuture<Void>> func);
}
