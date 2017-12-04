package com.fabrikam.dronedelivery.ingestion.models;

import java.util.Date;

public interface DeliveryBase {

	String getDeliveryId();

	String getDropOffLocation();

	String getPickupLocation();
	
	Date getPickupTime();
}