package com.fabrikam.dronedelivery.ingestion.service;

import java.util.Map;
import com.fabrikam.dronedelivery.ingestion.models.*;

import org.springframework.scheduling.annotation.Async;

public interface Ingestion {

	@Async
	public void scheduleDeliveryAsync(DeliveryBase delivery, Map<String,String> headers);

	@Async
	public void cancelDeliveryAsync(String deliveryId, Map<String,String> headers);

	@Async
	public void rescheduleDeliveryAsync(DeliveryBase rescheduledDelivery, Map<String, String> map);

}
