package com.fabrikam.dronedelivery.ingestion.service;
import com.fabrikam.dronedelivery.ingestion.models.*;

import java.nio.charset.Charset;
import java.util.HashMap;
import java.util.Map;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

import com.fabrikam.dronedelivery.ingestion.configuration.ApplicationProperties;
import com.fabrikam.dronedelivery.ingestion.util.ClientPool;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import com.microsoft.azure.servicebus.Message;
import com.microsoft.azure.servicebus.primitives.ServiceBusException;

@Service
public class IngestionImpl implements Ingestion {

	@Autowired
	private ClientPool clientPool;
	
	@Autowired
	private ApplicationProperties appProps;

	@Autowired
	public IngestionImpl(ClientPool clientPool, ApplicationProperties appProps) {
		this.clientPool = clientPool;
		this.appProps = appProps;
	}

	@Async
	@Override
	public void scheduleDeliveryAsync(DeliveryBase delivery, Map<String,String> httpHeaders) {
		Message sendEvent = getMessage(delivery, httpHeaders);
		sendEvent.getProperties().put("operation", "delivery");
		try {
			clientPool.getConnection().sendAsync(sendEvent).thenApply((Void) -> "result");
		} catch (InterruptedException | ServiceBusException e) {
			throw new RuntimeException(e);
		}
	}

	@Async
	@Override
	public void cancelDeliveryAsync(String deliveryId, Map<String,String> httpHeaders) {
		Message sendEvent = getMessage(deliveryId, httpHeaders);

		sendEvent.getProperties().put("operation", "cancel");
		try {
			clientPool.getConnection().sendAsync(sendEvent).thenApply((Void) -> "result");
		} catch (InterruptedException | ServiceBusException e) {
			throw new RuntimeException(e);

		}
	}

	@Async
	@Override
	public void rescheduleDeliveryAsync(DeliveryBase rescheduledDelivery, Map<String,String> httpHeaders) {
		Message sendEvent = getMessage(rescheduledDelivery, httpHeaders);
		sendEvent.getProperties().put("operation", "reschedule");
		try {
			clientPool.getConnection().sendAsync(sendEvent).thenApply((Void) -> "result");
		} catch (InterruptedException | ServiceBusException e) {
			throw new RuntimeException(e);
		}
	}
	
	private Message getMessage(Object deliveryObj, Map<String,String> httpHeaders){
		Gson gson = new GsonBuilder().create();
		byte[] payloadBytes = gson.toJson(deliveryObj).getBytes(Charset.defaultCharset());
		Message sendEvent = new Message(payloadBytes);
		
		Map<String,String> eventProps = new HashMap<String, String>();
		for(String header:appProps.getServiceMeshHeaders()){
			if(httpHeaders.containsKey(header)){
				eventProps.put(header, httpHeaders.get(header));
			}
		}
		
		if(eventProps.size()>0){
			sendEvent.getProperties().putAll(eventProps);
		}
		
		return sendEvent;
	}
}
