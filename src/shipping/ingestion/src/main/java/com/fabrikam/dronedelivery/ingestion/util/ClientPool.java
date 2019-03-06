package com.fabrikam.dronedelivery.ingestion.util;

import org.springframework.scheduling.annotation.Async;

import java.net.URISyntaxException;

import com.microsoft.azure.servicebus.primitives.ServiceBusException;

public interface ClientPool {

	@Async
	public InstrumentedQueueClient getConnection() throws InterruptedException, ServiceBusException, URISyntaxException;
}
