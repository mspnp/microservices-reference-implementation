package com.fabrikam.dronedelivery.ingestion.util;

import org.springframework.scheduling.annotation.Async;

import com.microsoft.azure.servicebus.QueueClient;
import com.microsoft.azure.servicebus.primitives.ServiceBusException;

public interface ClientPool {

	@Async
	public QueueClient getConnection() throws InterruptedException, ServiceBusException;

}
