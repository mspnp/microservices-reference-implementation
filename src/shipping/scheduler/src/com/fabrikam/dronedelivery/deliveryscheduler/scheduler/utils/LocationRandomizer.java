package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils;

import java.util.Random;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.Location;

public class LocationRandomizer {

	private static Random random = new Random();

	public static Location getRandomLocation() {
		Location location = new Location();
		location.setAltitude(random.nextDouble());
		location.setLatitude(random.nextDouble());
		location.setLongitude(random.nextDouble());

		return location;
	}
}
