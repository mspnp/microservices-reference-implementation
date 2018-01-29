package com.fabrikam.dronedelivery.ingestion.models;

import java.util.Date;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.Scope;
import org.springframework.stereotype.Component;

@Component
@Qualifier("Deliverybean")
@Scope("prototype")
public class Delivery implements DeliveryBase {

	private String deliveryId;
	private String ownerId;
	private String pickupLocation;
	private String dropoffLocation;
	private Date pickupTime;
	private Boolean expedited;
	private String deadline;
	private ConfirmationRequired confirmationRequired;
	private PackageInfo packageInfo;

	@Autowired

	public Delivery(String deliveryId, String ownerId, String pickupLocation, String dropoffLocation, Date pickupTime,
			Boolean expedited, ConfirmationRequired confirmationRequired, PackageInfo packageInfo, String deadline) {

		this.deliveryId = deliveryId;
		this.ownerId = ownerId;
		this.pickupLocation = pickupLocation;
		this.dropoffLocation = dropoffLocation;
		this.pickupTime = pickupTime;
		this.expedited = expedited;
		this.confirmationRequired = confirmationRequired;
		this.setPackageInfo(packageInfo);
		this.deadline = deadline;
	}

	public String getOwnerId() {
		return this.ownerId;
	}

	/* (non-Javadoc)
	 * @see com.fabrikam.dronedelivery.deliveryscheduler.model.DeliveryBase#getDeliveryId()
	 */
	@Override
	public String getDeliveryId() {
		return this.deliveryId;
	}

	/* (non-Javadoc)
	 * @see com.fabrikam.dronedelivery.deliveryscheduler.model.DeliveryBase#getPickupTime()
	 */
	@Override
	public Date getPickupTime() {
		return this.pickupTime;
	}

	/* (non-Javadoc)
	 * @see com.fabrikam.dronedelivery.deliveryscheduler.model.DeliveryBase#getDropOffLocation()
	 */
	@Override
	public String getDropOffLocation() {
		return this.dropoffLocation;
	}

	/* (non-Javadoc)
	 * @see com.fabrikam.dronedelivery.deliveryscheduler.model.DeliveryBase#getPickupLocation()
	 */
	@Override
	public String getPickupLocation() {
		return this.pickupLocation;
	}

	public ConfirmationRequired getConfirmationRequired() {
		return this.confirmationRequired;
	}

	public void setConfirmationRequired(ConfirmationRequired confReq) {
		this.confirmationRequired = confReq;
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

	public PackageInfo getPackageInfo() {
		return packageInfo;
	}

	public void setPackageInfo(PackageInfo packageInfo) {
		this.packageInfo = packageInfo;
	}
}
