package com.fabrikam.dronedelivery.deliveryscheduler.akkareader;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.microsoft.azure.iot.iothubreact.MessageFromDevice;

/*
 * AkkaDelivery aids in flowing the original message received from event hub
 * through the fluent akka api, eventually being used for checkpointing.
 */
public class AkkaDelivery {
	private Delivery delivery;
	private MessageFromDevice messageFromDevice;

	public Delivery getDelivery() {
		return delivery;
	}

	public void setDelivery(Delivery delivery) {
		this.delivery = delivery;
	}

	public MessageFromDevice getMessageFromDevice() {
		return messageFromDevice;
	}

	public void setMessageFromDevice(MessageFromDevice messageFromDevice) {
		this.messageFromDevice = messageFromDevice;
	}
}
