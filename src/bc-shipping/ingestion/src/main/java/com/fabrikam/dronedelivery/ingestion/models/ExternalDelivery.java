package com.fabrikam.dronedelivery.ingestion.models;

import java.util.Date;
import org.springframework.stereotype.Component;

@Component
public class ExternalDelivery {

	private String deliveryId;
	private String ownerId;
	private String pickupLocation;
	private String dropoffLocation;

	private Date pickupTime;
	private String deadline;
	private Boolean expedited;
	private ConfirmationRequired confirmationRequired;
	private PackageInfo packageInfo;

	public String getOwnerId() {
		return this.ownerId;
	}

	public void setOwnerId(String OwnerId) {
		this.ownerId = OwnerId;
	}

	public String getDeliveryId() {
		return this.deliveryId;
	}

	public void setDeliveryId(String deliveryId) {
		this.deliveryId = deliveryId;
	}

	public Date getPickupTime() {
		return this.pickupTime;
	}

	public void setPickupTime(Date pickupTime) {
		this.pickupTime = pickupTime;
	}

	public String getDropOffLocation() {
		return this.dropoffLocation;
	}

	public void setDropOffLocation(String dropoffLocation) {
		this.dropoffLocation = dropoffLocation;
	}

	public String getPickupLocation() {
		return this.pickupLocation;
	}

	public void setPickupLocation(String pickUpLocation) {
		this.pickupLocation = pickUpLocation;
	}

	public PackageInfo getPackageInfo() {
		return this.packageInfo;
	}

	public void setPackageInfo(PackageInfo packageInfo) {
		this.packageInfo = packageInfo;
	}

	public Boolean isExpedited() {
		return this.expedited;
	}

	public void setExpedited(Boolean expedited) {
		this.expedited = expedited;
	}

	public String getDeadline() {
		return deadline;
	}

	public void setDeadline(String deadline) {
		this.deadline = deadline;
	}

	public ConfirmationRequired getConfirmationRequired() {
		return confirmationRequired;
	}

	public void setConfirmationRequired(ConfirmationRequired confirmationRequired) {
		this.confirmationRequired = confirmationRequired;
	}

	@Override
	public String toString() {
		return "ExternalDelivery [deliveryId=" + deliveryId + ", ownerId=" + ownerId + ", pickupLocation="
				+ pickupLocation + ", dropoffLocation=" + dropoffLocation + ", pickupTime=" + pickupTime + ", deadline="
				+ deadline + ", expedited=" + expedited + ", confirmationRequired=" + confirmationRequired.name()
				+ ", packageInfo=" + packageInfo.toString() + "]";
	}
}
