package com.fabrikam.dronedelivery.ingestion.util;

import com.fabrikam.dronedelivery.ingestion.configuration.*;

import com.microsoft.azure.servicebus.QueueClient;
import com.microsoft.azure.servicebus.ReceiveMode;
import com.microsoft.azure.servicebus.primitives.ConnectionStringBuilder;
import com.microsoft.azure.servicebus.primitives.ServiceBusException;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

@Service
public class ClientPoolImpl implements ClientPool {

	private final QueueClient[] queueClients;
	private final String[] queueNames;
	private final ApplicationProperties appProperties;
	private final String nameSpace;
	private final String sasKeyName;
	private final String sasKey;
	

	@Autowired
	public ClientPoolImpl(ApplicationProperties appProps) {
		this.appProperties = appProps;
		
		
		this.queueNames = System.getenv(appProperties.getEnvQueueName()).split(",");		
		nameSpace = System.getenv(appProperties.getEnvNameSpace());					
		sasKeyName = System.getenv(appProperties.getEnvsasKeyName());
		sasKey = System.getenv(appProperties.getEnvsasKey());
			
		this.queueClients = new QueueClient[this.appProperties.getMessageAmqpClientPoolSize()];
	}

	@Async
	@Override
	public QueueClient getConnection() throws InterruptedException, ServiceBusException {

		int poolId = (int) (Math.random() * queueClients.length);
		int eventHubId = (int) (Math.random() * queueNames.length);

		if (queueClients[poolId] == null) {
			ConnectionStringBuilder connectionString = new ConnectionStringBuilder(nameSpace,
				queueNames[eventHubId], sasKeyName, sasKey);
			queueClients[poolId] = new QueueClient(connectionString, ReceiveMode.PEEKLOCK);
		}
	
		return queueClients[poolId];
	}
}
