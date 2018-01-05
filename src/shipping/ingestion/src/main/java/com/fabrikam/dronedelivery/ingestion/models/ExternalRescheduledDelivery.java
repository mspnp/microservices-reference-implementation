package com.fabrikam.dronedelivery.ingestion.models;

import java.util.Date;

public class ExternalRescheduledDelivery {

	private String deliveryId;
	private String pickupLocation;
	private String dropoffLocation;
	private String deadline;
	private Date pickupTime;

	public String getDeliveryId() {
		return this.deliveryId;
	}

	public void setDeliveryId(String deliveryId) {
		this.deliveryId = deliveryId;
	}

	public String getPickupLocation() {
		return this.pickupLocation;
	}

	public void setPickupLocation(String pickUpLocation) {
		this.pickupLocation = pickUpLocation;
	}

	public String getDropOffLocation() {
		return this.dropoffLocation;
	}

	public void setDropOffLocation(String dropoffLocation) {
		this.dropoffLocation = dropoffLocation;
	}

	public String getDeadline() {
		return this.deadline;
	}

	public void setDeadline(String deadline) {
		this.deadline = deadline;
	}

	public Date getPickupTime() {
		return pickupTime;
	}

	public void setPickupTime(Date pickupTime) {
		this.pickupTime = pickupTime;
	}

	@Override
	public String toString() {
		return "ExternalRescheduledDelivery [deliveryId=" + deliveryId + ", pickupLocation=" + pickupLocation
				+ ", dropoffLocation=" + dropoffLocation + ", deadline=" + deadline + ", pickupTime=" + pickupTime
				+ "]";
	}
}
