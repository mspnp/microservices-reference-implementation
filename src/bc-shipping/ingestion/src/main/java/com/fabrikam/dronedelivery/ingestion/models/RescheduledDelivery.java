package com.fabrikam.dronedelivery.ingestion.models;

import java.util.Date;

import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.Scope;
import org.springframework.stereotype.Component;

@Component
@Qualifier("RescheduledDeliveryBean")
@Scope("prototype")
public class RescheduledDelivery implements DeliveryBase  {

	private String deliveryId;
	private String pickupLocation;
	private String dropoffLocation;
	private String deadline;
	private Date pickupTime;

	public RescheduledDelivery(String deliveryId, 
			String pickupLocation, 
			String dropoffLocation, 
			String deadline, 
			Date pickupTime) {

		this.deliveryId = deliveryId;
		this.pickupLocation = pickupLocation;
		this.dropoffLocation = dropoffLocation;
		this.deadline = deadline;
		this.pickupTime = pickupTime;

	}

	@Override
	public String getDeliveryId() {
		return this.deliveryId;
	}

	@Override
	public String getPickupLocation() {
		return this.pickupLocation;
	}

	@Override
	public Date getPickupTime() {
		return pickupTime;
	}

	@Override
	public String getDropOffLocation() {
		return this.dropoffLocation;
	}

	public String getDeadline() {
		return deadline;
	}

}
