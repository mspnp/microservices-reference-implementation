package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.compensation;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.SchedulerSettings;
import com.microsoft.azure.storage.CloudStorageAccount;
import com.microsoft.azure.storage.queue.CloudQueueClient;

import java.net.URISyntaxException;
import java.security.InvalidKeyException;

import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

public class StorageQueueClientFactory {

    private static final Logger Log = LogManager.getLogger(StorageQueueClientFactory.class);

    private static CloudQueueClient queueClient;

    static {

        try {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.parse(SchedulerSettings.storageQueueConnectionString);
            queueClient = cloudStorageAccount.createCloudQueueClient();
        } catch (URISyntaxException | InvalidKeyException e) {
            Log.error("throwable: {}", ExceptionUtils.getStackTrace(e).toString());
        }
    }

    public static CloudQueueClient get() {

        return queueClient;

    }


}
