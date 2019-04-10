package com.fabrikam.dronedelivery.ingestion;

import static org.junit.Assert.*;

import org.junit.Test;
import com.fabrikam.dronedelivery.ingestion.configuration.ApplicationProperties;

public class ApplicationPropertiesTest {
	@Test
	public void canSetPropertyValuesUsedByClientPoolImpl_thenGetPropertyValues() {
		// Arrange
		ApplicationProperties appProps = new ApplicationProperties();

		// Act
		appProps.setEnvQueueName("ENV_QUEUE_NAME_MODIFIED");
		appProps.setEnvNameSpace("ENV_QUEUE_NS_MODIFIED");
		appProps.setEnvsasKeyName("ENV_KEY_NAME_MODIFIED");
		appProps.setEnvsasKey("ENV_KEY_VALUE_MODIFIED");

		// Assert
		assertEquals("ENV_QUEUE_NAME_MODIFIED", appProps.getEnvQueueName());
		assertEquals("ENV_QUEUE_NS_MODIFIED", appProps.getEnvNameSpace());
		assertEquals("ENV_KEY_NAME_MODIFIED", appProps.getEnvsasKeyName());
		assertEquals("ENV_KEY_VALUE_MODIFIED", appProps.getEnvsasKey());
	}
}
