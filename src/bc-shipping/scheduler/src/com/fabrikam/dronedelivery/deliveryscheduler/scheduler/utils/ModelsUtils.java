package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils;

import java.util.Date;
import java.util.Random;
import java.util.UUID;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.Location;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageGen;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ConfirmationRequired;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ContainerSize;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

public class ModelsUtils {
	private final static Random random = new Random();

	private final static JsonParser jsonParser = new JsonParser();
	
	private final static ContainerSize[] containers = ContainerSize.values();
	
	private final static String Locations[] = { "Austin", "Seattle", "Berkley", "Oregon", "Florida", "Blaine", "Renton" };

	private final static ConfirmationRequired[] confirmations = ConfirmationRequired.values();

	public static Location getRandomLocation(double... arguments) {
		Location location = null;
		if (arguments != null && arguments.length >= 1) {
			double x = arguments[0];
			location = new Location(x, x, x);
		} else {
			location = new Location();
			location.setAltitude(random.nextDouble());
			location.setLatitude(random.nextDouble());
			location.setLongitude(random.nextDouble());
		}

		return location;
	}
	
	public static PackageGen getPackageGen(String jsonStr) {
		PackageGen pack = new PackageGen();
		JsonElement jsonElem = jsonParser.parse(jsonStr);
		JsonObject jObject = jsonElem.getAsJsonObject();
		pack.setId(jObject.get("id").getAsString());
		pack.setSize(ContainerSize.valueOf(jObject.get("size").getAsString()));
		pack.setTag(jObject.get("tag").getAsString());

		JsonElement weight = jObject.get("weight");
		pack.setWeight(weight.isJsonNull() ? 0.0 : weight.getAsDouble());

		return pack;
	}
	
	public static PackageInfo getPackageInfo(String randomTag){
		PackageInfo packInfo = new PackageInfo();

		packInfo.setPackageId(UUID.randomUUID().toString());
		packInfo.setSize(containers[random.nextInt(containers.length)]);
		packInfo.setTag(randomTag);
		packInfo.setWeight(1.0);
		
		return packInfo;
	}
	
	public static Delivery createDeliveryRequest() {
		PackageInfo pack = new PackageInfo();

		pack.setPackageId(UUID.randomUUID().toString());
		pack.setSize(containers[random.nextInt(containers.length)]);

		Delivery delivery = new Delivery();

		delivery.setDeliveryId(UUID.randomUUID().toString());
		delivery.setOwnerId(UUID.randomUUID().toString());
		delivery.setPickupTime(new Date());
		delivery.setDropOffLocation(Locations[random.nextInt(Locations.length)]);
		delivery.setPickupLocation(Locations[random.nextInt(Locations.length)]);
		delivery.setConfirmationRequired(confirmations[random.nextInt(confirmations.length)]);
		delivery.setDeadline("LineOfDeadPeople");
		delivery.setExpedited(true);

		delivery.setPackageInfo(pack);

		return delivery;
	}
}
