package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.compensation;

import java.net.URISyntaxException;

import org.junit.Assert;
import org.junit.Before;
import org.junit.Ignore;
import org.junit.Rule;
import org.junit.Test;
import org.junit.contrib.java.lang.system.EnvironmentVariables;

import com.microsoft.azure.storage.StorageException;
import com.microsoft.azure.storage.queue.CloudQueue;
import com.microsoft.azure.storage.queue.CloudQueueClient;
import com.microsoft.azure.storage.queue.CloudQueueMessage;

public class StorageQueueTest {
	@Rule
	public final EnvironmentVariables environmentVariables = new EnvironmentVariables();

	private static final String envStorageConnectionString = "STORAGE_QUEUE_CONNECTION_STRING";
	private static final String envStorageQueueName = "STORAGE_QUEUE_NAME";

	@Before
	public void setup() {
		environmentVariables.set(envStorageConnectionString, "<storage-connection-string>");
		environmentVariables.set(envStorageQueueName, "<storage-queue-name>");
	}

	/*
	 * Ignoring for Maven build since storage connection and queue name needs to be set to run this test
	 */
	@Ignore
	@Test
	public void can_add_message_to_queue() throws URISyntaxException, StorageException {
		// Arrange
		String connStr = System.getenv(envStorageConnectionString);
		CloudQueueClient storageClient = StorageQueueClientFactory.INSTANCE.get(connStr);
		String someStringContent = "This is test message to queue";
		CloudQueueMessage queueMessage = new CloudQueueMessage(someStringContent);

		CloudQueue queueReference = storageClient.getQueueReference(System.getenv(envStorageQueueName));
		queueReference.createIfNotExists();

		// Act
		queueReference.addMessage(queueMessage);

		CloudQueueMessage peekMessage = queueReference.peekMessage();

		// Assert
		Assert.assertTrue(peekMessage.getMessageContentAsString().equalsIgnoreCase(someStringContent));
	}

}
