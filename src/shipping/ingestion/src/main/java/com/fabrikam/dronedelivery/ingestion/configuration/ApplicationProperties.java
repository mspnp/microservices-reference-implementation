package com.fabrikam.dronedelivery.ingestion.configuration;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.support.PropertySourcesPlaceholderConfigurer;

/**
 * This declares that this object can be bound to the "eventhub" prefix in the
 * {@link org.springframework.core.env.Environment}. By setting the properties
 * in application.configuration file will override all values from the class to
 * make it work 1. configure as a regular bean
 * {@link org.springframework.boot.context.properties.EnableConfigurationProperties}
 * to your {@code @Configuration} class. 2. specify
 * {@code @EnableConfigurationProperties(EventHubProperties.class} and Spring
 * Boot will create a bean automatically for you
 * 
 */

@ConfigurationProperties("service")
public class ApplicationProperties {

	// the properties are overriden by values
	// in application.properties

	// Queue properties
	private String namespace = "queueNamespace";
	private String queueName = "queueName";
	private String sasKeyName = "sasKeyName";
	private String sasKey = "sasKey";

	private String envNameSpace = "ENV_QUEUE_NS";
	private String envQueueName = "ENV_QUEUE_NAME";
	private String envsasKeyName = "ENV_KEY_NAME";
	private String envsasKey = "ENV_KEY_VALUE";
	
	// Threadpool properties
	private int threadPoolExecutorPoolSize = 0;
	private int threadPoolExecutorQueueSize = 0;
	private int threadPoolExecutorMaxPoolSize = 0;
	private int messageAmqpClientPoolSize = 0;

	public String getNamespace() {
		return namespace;
	}

	public void setNamespace(String nameSpace) {
		this.namespace = nameSpace;
	}

	public String getQueueName() {
		return queueName;
	}

	public void setQueueName(String queueName) {
		this.queueName = queueName;
	}

	public String getSasKeyName() {
		return sasKeyName;
	}

	public void setSasKeyName(String sasKeyName) {
		this.sasKeyName = sasKeyName;
	}

	public String getSasKey() {
		return sasKey;
	}

	public void setSasKey(String sasKey) {
		this.sasKey = sasKey;
	}

	public int getThreadPoolExecutorPoolSize() {
		return threadPoolExecutorPoolSize;
	}

	public void setThreadPoolExecutorPoolSize(int poolSize) {
		this.threadPoolExecutorPoolSize = poolSize;
	}

	public int getThreadPoolExecutorQueueSize() {
		return threadPoolExecutorQueueSize;
	}

	public void setThreadPoolExecutorQueueSize(int queueSize) {
		this.threadPoolExecutorQueueSize = queueSize;
	}

	public int getThreadPoolExecutorMaxPoolSize() {
		return threadPoolExecutorMaxPoolSize;
	}

	public void setThreadPoolExecutorMaxPoolSize(int maxPoolSize) {
		this.threadPoolExecutorMaxPoolSize = maxPoolSize;
	}
		
	//To resolve ${} in @Value
	@Bean
	public static PropertySourcesPlaceholderConfigurer propertyConfigInDev() {
		return new PropertySourcesPlaceholderConfigurer();
	}

	public int getMessageAmqpClientPoolSize() {
		return messageAmqpClientPoolSize;
	}

	public void setMessageAmqpClientPoolSize(int messageAmqpClientPoolSize) {
		this.messageAmqpClientPoolSize = messageAmqpClientPoolSize;
	}

	public String getEnvNameSpace() {
		return envNameSpace;
	}

	public void setEnvNameSpace(String envNameSpace) {
		this.envNameSpace = envNameSpace;
	}

	public String getEnvQueueName() {
		return envQueueName;
	}

	public void setEnvQueueName(String envQueueName) {
		this.envQueueName = envQueueName;
	}

	public String getEnvsasKeyName() {
		return envsasKeyName;
	}

	public void setEnvsasKeyName(String envsasKeyName) {
		this.envsasKeyName = envsasKeyName;
	}

	public String getEnvsasKey() {
		return envsasKey;
	}

	public void setEnvsasKey(String envsasKey) {
		this.envsasKey = envsasKey;
	}
}
