package com.fabrikam.dronedelivery.ingestion.configuration;

import com.microsoft.applicationinsights.TelemetryClient;

import org.mockito.Mockito;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;

@Configuration
public class TestAppConfig {

    @Bean
    @Primary
    public TelemetryClient getTelemetryClient() {
        return Mockito.mock(TelemetryClient.class);
    }
}
