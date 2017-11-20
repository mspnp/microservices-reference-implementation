package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver;

import java.util.Date;

public class Delivery {
	private String deliveryId;
	private String ownerId;
	private String pickupLocation;
	private String dropoffLocation;
	private String deadline;
	private Boolean expedited;
	private ConfirmationRequired confirmationRequired;

	private Date pickupTime;

	private PackageInfo packageInfo;

	public Date getPickupTime() {
		return this.pickupTime;
	}
	
	public void setPickupTime(Date pickupTime){
		this.pickupTime = pickupTime;
	}

	public String getDropOffLocation() {
		return this.dropoffLocation;
	}
	
	public void setDropOffLocation(String dropoffLocation){
		this.dropoffLocation = dropoffLocation;
	}

	public String getPickupLocation() {
		return this.pickupLocation;
	}
	
	public void setPickupLocation(String pickupLocation){
		this.pickupLocation = pickupLocation;
	}

	public ConfirmationRequired getConfirmationRequired() {
		return this.confirmationRequired;
	}

	public void setConfirmationRequired(ConfirmationRequired confReq) {
		this.confirmationRequired = confReq;
	}

	public Boolean isExpedited() {
		return expedited;
	}

	public void setExpedited(Boolean expedited) {
		this.expedited = expedited;
	}

	public String getDeadline() {
		return this.deadline;
	}

	public void setDeadline(String deadline) {
		this.deadline = deadline;
	}

	public String getDeliveryId() {
		return deliveryId;
	}

	public void setDeliveryId(String deliveryId) {
		this.deliveryId = deliveryId;
	}

	public String getOwnerId() {
		return ownerId;
	}

	public void setOwnerId(String ownerId) {
		this.ownerId = ownerId;
	}

	@Override
	public String toString() {
		return String.format(
				"DeliveryId:%s, OwnerId:%s, Locations [Pickup:%s, Dropoff:%s], PickupTime:%tc, Package:%s, ConfirmationType:%s, Deadline:%s, Expedited:%s",
				this.deliveryId, this.ownerId, this.pickupLocation, this.dropoffLocation, this.pickupTime,
				this.packageInfo.toString(), this.confirmationRequired.name(), this.deadline, this.expedited);
	}

	public PackageInfo getPackageInfo() {
		return packageInfo;
	}

	public void setPackageInfo(PackageInfo packageInfo) {
		this.packageInfo = packageInfo;
	}
}
