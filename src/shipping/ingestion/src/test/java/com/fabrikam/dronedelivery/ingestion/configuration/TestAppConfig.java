package com.fabrikam.dronedelivery.ingestion.configuration;

import com.fabrikam.dronedelivery.ingestion.util.ClientPool;
import com.fabrikam.dronedelivery.ingestion.util.ClientPoolImpl;
import com.fabrikam.dronedelivery.ingestion.util.InstrumentedQueueClient;
import com.fabrikam.dronedelivery.ingestion.util.Environment;

import com.microsoft.applicationinsights.TelemetryClient;
import com.microsoft.azure.servicebus.IMessage;
import com.microsoft.azure.servicebus.primitives.ServiceBusException;

import static org.mockito.Mockito.*;

import java.net.URISyntaxException;
import java.util.concurrent.CompletableFuture;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;

@Configuration
public class TestAppConfig {

    @Autowired
	private ApplicationProperties appProperites;

    @Bean
    @Primary
    public TelemetryClient getTelemetryClient() {
        return mock(TelemetryClient.class);
    }

    @Bean
    @Primary
    public Environment getEnvironment() {
        Environment envMock = mock(Environment.class);

        when(envMock.getenv(appProperites.getEnvQueueName()))
            .thenReturn("test-queue");

        return envMock;
    }

    @Bean
    @Primary
    public ClientPool getClientPool() throws InterruptedException, ServiceBusException, URISyntaxException {
        ClientPool clientPoolMock = mock(ClientPoolImpl.class);
        InstrumentedQueueClient instrumentedQueueClient =
            mock(InstrumentedQueueClient.class);
        CompletableFuture<Void> futureMock =
            (CompletableFuture<Void>) mock(CompletableFuture.class);

        when(instrumentedQueueClient.sendAsync(any(IMessage.class)))
            .thenReturn(futureMock);
        when(clientPoolMock.getConnection())
            .thenReturn(instrumentedQueueClient);

        return clientPoolMock;
    }
}
