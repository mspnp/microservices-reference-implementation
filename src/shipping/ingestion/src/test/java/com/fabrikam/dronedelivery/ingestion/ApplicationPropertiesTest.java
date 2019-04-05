package com.fabrikam.dronedelivery.ingestion;

import static org.junit.Assert.*;
import static org.mockito.Mockito.times;

import org.junit.Before;
import org.junit.Test;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.support.PropertySourcesPlaceholderConfigurer;
import com.fabrikam.dronedelivery.ingestion.configuration.ApplicationProperties;

public class ApplicationPropertiesTest {
	
	public class Environment {
	    public String getenv(String envName) {
	        return System.getenv(envName); 
	    }
	}
	
	
	 @Configuration
	    public static class Config {
	    @Bean
	        public static PropertySourcesPlaceholderConfigurer propertyConfigurer() {
	            return new PropertySourcesPlaceholderConfigurer();
	        }
	    }
		  
	  
	@Before
	public void setUp() throws Exception {			
		MockitoAnnotations.initMocks(this);		    
	}

	@Test
	public void canGetPropertiesfromEnvVariables() {
		Environment systemMock = Mockito.mock(Environment.class);
		Mockito.when(systemMock.getenv("ENV_QUEUE_NAME")).thenReturn("variableMock");
		ApplicationProperties appProps = new ApplicationProperties();
		assertEquals(systemMock.getenv(appProps.getEnvQueueName()),"variableMock");
		Mockito.verify(systemMock, times(1))
		 .getenv(appProps.getEnvQueueName());		
	}

}
