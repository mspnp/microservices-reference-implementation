package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.compensation.tests;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.SchedulerSettings;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.compensation.StorageQueueClientFactory;
import com.microsoft.azure.storage.queue.CloudQueue;
import com.microsoft.azure.storage.queue.CloudQueueClient;
import com.microsoft.azure.storage.queue.CloudQueueMessage;
import org.junit.Assert;
import org.junit.Before;
import org.junit.Test;


public class StorageQueueTest {

    private CloudQueueClient storageClient ;

    private String someStringContent;

//    This test will work only if environment variables are set
//    STORAGE_QUEUE_CONNECTION_STRING and STORAGE_QUEUE_NAME

    @Before
    public void setup() {

        SchedulerSettings.StorageQueueConnectionString = System.getenv("STORAGE_QUEUE_CONNECTION_STRING");
        SchedulerSettings.StorageQueueName = System.getenv("STORAGE_QUEUE_NAME");
        storageClient = StorageQueueClientFactory.get();
        someStringContent = "This is test message to queue";
    }

    @Test
    public void it_should_add_message_to_queue()  {

        //Arrange
        CloudQueueMessage queueMessage = new CloudQueueMessage(someStringContent);

        CloudQueue queueReference = null;
        try {

            queueReference = storageClient.getQueueReference(SchedulerSettings.StorageQueueName);
            queueReference.createIfNotExists();


            //Act
            queueReference.addMessage(queueMessage);

            CloudQueueMessage peekMessage = queueReference.peekMessage();


            //Assert
            Assert.assertTrue(peekMessage.getMessageContentAsString().equalsIgnoreCase(someStringContent));
        } catch (Exception e) {
            e.printStackTrace();
        }
    }




}
