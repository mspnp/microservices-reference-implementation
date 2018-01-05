package com.fabrikam.dronedelivery.ingestion.util;

import com.fabrikam.dronedelivery.ingestion.configuration.*;
import com.microsoft.azure.eventhubs.EventHubClient;
import com.microsoft.azure.servicebus.ConnectionStringBuilder;
import com.microsoft.azure.servicebus.ServiceBusException;

import java.io.IOException;
import java.util.concurrent.ExecutionException;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

@Service
public class ClientPoolImpl implements ClientPool {

	private final EventHubClient[] eventHubClients;
	private final String[] eventHubNames;
	private final ApplicationProperties appProperties;
	private final String nameSpace;
	private final String sasKeyName;
	private final String sasKey;
	

	@Autowired
	public ClientPoolImpl(ApplicationProperties appProps)
			throws IOException, ServiceBusException, InterruptedException, ExecutionException {
		this.appProperties = appProps;
		
		
		this.eventHubNames = System.getenv(appProperties.getEnvHubName()).split(",");		
		nameSpace = System.getenv(appProperties.getEnvNameSpace());					
		sasKeyName = System.getenv(appProperties.getEnvsasKeyName());
		sasKey = System.getenv(appProperties.getEnvsasKey());
			
		this.eventHubClients = new EventHubClient[this.appProperties.getMessageAmqpClientPoolSize()];
	}

	@Async
	@Override
	public EventHubClient getConnection()
			throws InterruptedException, ExecutionException, ServiceBusException, IOException {

		int poolId = (int) (Math.random() * eventHubClients.length);
		int eventHubId = (int) (Math.random() * eventHubNames.length);

		if (eventHubClients[poolId] == null) {
			ConnectionStringBuilder connectionString = new ConnectionStringBuilder(nameSpace,
					eventHubNames[eventHubId], sasKeyName, sasKey);
			eventHubClients[poolId] = EventHubClient.createFromConnectionString(connectionString.toString()).get();
		}
		
		EventHubClient vclient = eventHubClients[poolId];

		return eventHubClients[poolId];
	}

}
