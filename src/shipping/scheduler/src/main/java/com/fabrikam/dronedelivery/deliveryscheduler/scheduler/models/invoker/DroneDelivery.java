package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker;

public class DroneDelivery {

	public DroneDelivery() {
		// TODO Auto-generated constructor stub
	}

	private String deliveryId;
	private Location pickup;
	private Location dropoff;
	private PackageDetail packageDetail;
	private boolean expedited;

	public String getDeliveryId() {
		return deliveryId;
	}

	public void setDeliveryId(String deliveryId) {
		this.deliveryId = deliveryId;
	}

	public Location getPickup() {
		return pickup;
	}

	public void setPickup(Location pickup) {
		this.pickup = pickup;
	}

	public Location getDropoff() {
		return dropoff;
	}

	public void setDropoff(Location dropoff) {
		this.dropoff = dropoff;
	}

	public boolean getExpedited() {
		return expedited;
	}

	public void setExpedited(boolean expedited) {
		this.expedited = expedited;
	}

	public PackageDetail getPackageDetail() {
		return packageDetail;
	}

	public void setPackageDetail(PackageDetail packageDetail) {
		this.packageDetail = packageDetail;
	}

}
