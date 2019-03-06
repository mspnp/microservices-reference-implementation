// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

package com.fabrikam.dronedelivery.ingestion.util;

import java.util.concurrent.CompletableFuture;

import com.microsoft.azure.servicebus.IMessage;
import com.microsoft.azure.servicebus.IQueueClient;

public class InstrumentedQueueClientImpl implements InstrumentedQueueClient {
	private final ServiceBusTracing tracing;
	private final String endpoint;
	private final IQueueClient client;

	public InstrumentedQueueClientImpl(
		String endpoint,  
		IQueueClient client,
		ServiceBusTracing tracing) {
		this.endpoint = endpoint;
		this.client = client;
		this.tracing = tracing;
	}

	@Override
	public CompletableFuture<Void> sendAsync(IMessage message) {
		String queueName = this.client.getQueueName();

		return this.tracing.trackAndCorrelateServiceBusDependency(
			endpoint,
			queueName,
			message,
			(m) ->
			{
				return this.client.sendAsync(m);
			});
	}
}
