package com.fabrikam.dronedelivery.ingestion.configuration;

import java.util.ArrayList;
import java.util.List;

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

	// Eventhub properties
	private String namespace = "eventhubNamespace";
	private String eventHubName = "eventHubName";
	private String sasKeyName = "sasKeyName";
	private String sasKey = "sasKey";

	private String envNameSpace = "ENV_HUB_NS";
	private String envHubName = "ENV_HUB_NAME";
	private String envsasKeyName = "ENV_KEY_NAME";
	private String envsasKey = "ENV_KEY_VALUE";
	
	// Threadpool properties
	private int threadPoolExecutorPoolSize = 0;
	private int threadPoolExecutorQueueSize = 0;
	private int threadPoolExecutorMaxPoolSize = 0;
	private int messageAmqpClientPoolSize = 0;
	
	// Istio properties for distributed tracing
	private List<String> serviceMeshHeaders = new ArrayList<String>();
	
	// Correlation header for breadcrumb trail
	private String serviceMeshCorrelationHeader = "serviceMeshCorrelationHeader";

	public String getNamespace() {
		return namespace;
	}

	public void setNamespace(String nameSpace) {
		this.namespace = nameSpace;
	}

	public String getEventHubName() {
		return eventHubName;
	}

	public void setEventHubName(String eventHubName) {
		this.eventHubName = eventHubName;
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
	
	public List<String> getServiceMeshHeaders() {
		return serviceMeshHeaders;
	}

	public void setServiceMeshHeaders(List<String> serviceMeshHeaders) {
		this.serviceMeshHeaders = serviceMeshHeaders;
	}
	
	//To resolve ${} in @Value
	@Bean
	public static PropertySourcesPlaceholderConfigurer propertyConfigInDev() {
		return new PropertySourcesPlaceholderConfigurer();
	}

	public String getServiceMeshCorrelationHeader() {
		return serviceMeshCorrelationHeader;
	}

	public void setServiceMeshCorrelationHeader(String serviceMeshCorrelationHeader) {
		this.serviceMeshCorrelationHeader = serviceMeshCorrelationHeader;
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

	public String getEnvHubName() {
		return envHubName;
	}

	public void setEnvHubName(String envHubName) {
		this.envHubName = envHubName;
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
