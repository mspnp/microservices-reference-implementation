package com.fabrikam.dronedelivery.ingestion.service;

import com.fabrikam.dronedelivery.ingestion.models.*;

import java.net.URISyntaxException;
import java.nio.charset.Charset;
import java.util.Map;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

import com.fabrikam.dronedelivery.ingestion.util.ClientPool;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.microsoft.azure.servicebus.Message;
import com.microsoft.azure.servicebus.primitives.ServiceBusException;

@Service
public class IngestionImpl implements Ingestion {

	private ClientPool clientPool;

	@Autowired
	public IngestionImpl(ClientPool clientPool) {
		this.clientPool = clientPool;
	}

	@Async
	@Override
	public void scheduleDeliveryAsync(DeliveryBase delivery, Map<String, String> httpHeaders) {
		Message sendEvent = getMessage(delivery, httpHeaders);
		sendEvent.getProperties().put("operation", "delivery");
		this.enqueuMessageAsync(sendEvent);
	}

	@Async
	@Override
	public void cancelDeliveryAsync(String deliveryId, Map<String, String> httpHeaders) {
		Message sendEvent = getMessage(deliveryId, httpHeaders);

		sendEvent.getProperties().put("operation", "cancel");
		this.enqueuMessageAsync(sendEvent);
	}

	@Async
	@Override
	public void rescheduleDeliveryAsync(DeliveryBase rescheduledDelivery, Map<String, String> httpHeaders) {
		Message sendEvent = getMessage(rescheduledDelivery, httpHeaders);
		sendEvent.getProperties().put("operation", "reschedule");
		this.enqueuMessageAsync(sendEvent);
	}

	private Message getMessage(Object deliveryObj, Map<String, String> httpHeaders) {
		Gson gson = new GsonBuilder().create();
		byte[] payloadBytes = gson.toJson(deliveryObj).getBytes(Charset.defaultCharset());
		Message sendEvent = new Message(payloadBytes);

		return sendEvent;
	}

	@Async
	private void enqueuMessageAsync(Message message) {
		try {
			this.clientPool.getConnection().sendAsync(message).thenApply((Void) -> "result");
		} catch (InterruptedException | ServiceBusException | URISyntaxException e) {
			throw new RuntimeException(e);
		} 
	}
}