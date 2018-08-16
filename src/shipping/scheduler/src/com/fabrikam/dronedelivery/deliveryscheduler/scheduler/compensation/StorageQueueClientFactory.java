package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.compensation;

import java.net.URISyntaxException;
import java.security.InvalidKeyException;

import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import com.microsoft.azure.storage.CloudStorageAccount;
import com.microsoft.azure.storage.queue.CloudQueueClient;
    
public enum StorageQueueClientFactory {
	INSTANCE;
	private static final Logger Log = LogManager.getLogger(StorageQueueClientFactory.class);

	public CloudQueueClient get(String connectionString) {
		CloudQueueClient queueClient = null;
		try {
			CloudStorageAccount cloudStorageAccount = CloudStorageAccount.parse(connectionString);
			queueClient = cloudStorageAccount.createCloudQueueClient();
		} catch (URISyntaxException | InvalidKeyException e) {
			Log.error("throwable: {}", ExceptionUtils.getStackTrace(e).toString());
		}
		return queueClient;
	}
}
