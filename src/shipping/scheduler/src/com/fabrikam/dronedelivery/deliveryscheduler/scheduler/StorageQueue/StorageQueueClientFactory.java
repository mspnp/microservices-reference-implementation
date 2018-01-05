package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.StorageQueue;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.SchedulerSettings;
import com.microsoft.azure.storage.CloudStorageAccount;
import com.microsoft.azure.storage.queue.CloudQueueClient;

import java.net.URISyntaxException;
import java.security.InvalidKeyException;

public class StorageQueueClientFactory {

    private static CloudQueueClient queueClient;

    static {

        try {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.parse(SchedulerSettings.storageQueueConnectionString);
            queueClient = cloudStorageAccount.createCloudQueueClient();
        } catch (URISyntaxException | InvalidKeyException e) {
            e.printStackTrace();
        }
    }

    public static CloudQueueClient get() {

        return queueClient;

    }


}
