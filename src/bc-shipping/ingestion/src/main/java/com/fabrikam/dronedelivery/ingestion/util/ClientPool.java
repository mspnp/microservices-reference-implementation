package com.fabrikam.dronedelivery.ingestion.util;

import java.io.IOException;
import java.util.concurrent.ExecutionException;

import org.springframework.scheduling.annotation.Async;

import com.microsoft.azure.eventhubs.EventHubClient;
import com.microsoft.azure.servicebus.ServiceBusException;

public interface ClientPool {

	@Async
	public EventHubClient getConnection()
			throws InterruptedException, ExecutionException, ServiceBusException, IOException;

}
