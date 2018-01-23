package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker;

public class Location {
	private double altitude;
	private double latitude;
	private double longitude;

	public Location(){
		super();
		this.altitude = 0.0;
		this.latitude = 0.0;
		this.longitude = 0.0;
	}
	
	public Location(double altitude, double latitude, double longitude) {
		super();
		this.altitude = altitude;
		this.latitude = latitude;
		this.longitude = longitude;
	}

	public double getAltitude() {
		return altitude;
	}

	public void setAltitude(double altitude) {
		this.altitude = altitude;
	}

	public double getLatitude() {
		return latitude;
	}

	public void setLatitude(double latitude) {
		this.latitude = latitude;
	}

	public double getLongitude() {
		return longitude;
	}

	public void setLongitude(double longitude) {
		this.longitude = longitude;
	}
}
