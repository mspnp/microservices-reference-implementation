package com.fabrikam.dronedelivery.ingestion.util;

import java.net.URI;
import java.net.URISyntaxException;

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

	private static final String SCHEME = "https";

	private final InstrumentedQueueClient[] queueClients;
	private final String[] queueNames;
	private final ApplicationProperties appProperties;
	private final String nameSpace;
	private final String sasKeyName;
	private final String sasKey;
	private final ServiceBusTracing tracing;

	@Autowired
	public ClientPoolImpl(ApplicationProperties appProps, ServiceBusTracing tracing, Environment environment) {
		this.appProperties = appProps;
		this.tracing = tracing;

		this.queueNames = environment.getenv(appProperties.getEnvQueueName()).split(",");
		nameSpace = environment.getenv(appProperties.getEnvNameSpace());
		sasKeyName = environment.getenv(appProperties.getEnvsasKeyName());
		sasKey = environment.getenv(appProperties.getEnvsasKey());

		this.queueClients = new InstrumentedQueueClient[this.appProperties.getMessageAmqpClientPoolSize()];
	}

	@Async
	@Override
	public InstrumentedQueueClient getConnection()
			throws InterruptedException, ServiceBusException, URISyntaxException {

		int poolId = (int) (Math.random() * queueClients.length);
		int eventHubId = (int) (Math.random() * queueNames.length);

		if (queueClients[poolId] == null) {
			ConnectionStringBuilder connectionString = new ConnectionStringBuilder(nameSpace, queueNames[eventHubId],
					sasKeyName, sasKey);

			queueClients[poolId] = new InstrumentedQueueClientImpl(
				new URI(SCHEME, 
					connectionString
						.getEndpoint()
						.getHost(), 
					"/", 
					null).toString(),
				new QueueClient(connectionString, ReceiveMode.PEEKLOCK),
				tracing);
		}
	
		return queueClients[poolId];
	}
}