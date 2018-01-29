package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker;

public class DeliverySchedule {
	private String Id;
	private UserAccount owner;
	private Location pickup;
	private Location dropoff;
	private String deadline;
	private Boolean expedited;
	private ConfirmationType confirmationRequired;
	private String droneId;

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

	public ConfirmationType getConfirmationRequired() {
		return confirmationRequired;
	}

	public void setConfirmationRequired(ConfirmationType confirmationRequired) {
		this.confirmationRequired = confirmationRequired;
	}

	public String getDroneId() {
		return droneId;
	}

	public void setDroneId(String droneId) {
		this.droneId = droneId;
	}

	public String getDeadline() {
		return this.deadline;
	}

	public void setDeadline(String deadline) {
		this.deadline = deadline;
	}

	public Boolean getExpedited() {
		return expedited;
	}

	public void setExpedited(Boolean expedited) {
		this.expedited = expedited;
	}

	public UserAccount getOwner() {
		return owner;
	}

	public void setOwner(UserAccount owner) {
		this.owner = owner;
	}

	public String getId() {
		return Id;
	}

	public void setId(String id) {
		Id = id;
	}

	@Override
	public String toString() {
		return "DeliverySchedule [Id=" + Id + ", owner=" + owner.toString() + ", pickup=" + pickup + ", dropoff=" + dropoff
				+ ", deadline=" + deadline + ", expedited=" + expedited + ", confirmationRequired="
				+ confirmationRequired.name() + ", droneId=" + droneId + "]"; //, packageId=" + packageId + "
	}
}
