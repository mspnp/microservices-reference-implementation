package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils;

import java.util.Date;
import java.util.Random;
import java.util.UUID;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DeliverySchedule;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DroneDelivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.Location;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageGen;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.UserAccount;
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
	
	private final static double leftLimit = 10D;
	
	private final static double rightLimit = 100D;
	

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
		pack.setId(jObject.get("packageId").getAsString());
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
		pack.setWeight(leftLimit + new Random().nextDouble() * (rightLimit - leftLimit));

		Delivery delivery = new Delivery();

		delivery.setDeliveryId(UUID.randomUUID().toString());
		delivery.setOwnerId(UUID.randomUUID().toString());
		delivery.setPickupTime(new Date());
		delivery.setDropOffLocation(Locations[random.nextInt(Locations.length)]);
		delivery.setPickupLocation(Locations[random.nextInt(Locations.length)]);
		delivery.setConfirmationRequired(confirmations[random.nextInt(confirmations.length)]);
		delivery.setDeadline("ByTheEndOfDay");
		delivery.setExpedited(true);

		delivery.setPackageInfo(pack);

		return delivery;
	}
	
	public static DroneDelivery createDroneDelivery(Delivery deliveryRequest) {
		DroneDelivery delivery = new DroneDelivery();
		delivery.setDeliveryId(deliveryRequest.getDeliveryId());

		delivery.setDropoff(ModelsUtils.getRandomLocation());
		delivery.setPickup(ModelsUtils.getRandomLocation());

		delivery.setExpedited(delivery.getExpedited());
		delivery.setPackageDetail(ModelsConverter.getPackageDetail(deliveryRequest.getPackageInfo()));

		return delivery;
	}
	
	public static DeliverySchedule createDeliverySchedule(Delivery deliveryRequest, String droneId) {
		UserAccount account = new UserAccount(UUID.randomUUID().toString(), deliveryRequest.getOwnerId());

		DeliverySchedule scheduleDelivery = new DeliverySchedule();
		scheduleDelivery.setId(deliveryRequest.getDeliveryId());
		scheduleDelivery.setOwner(account);
		scheduleDelivery.setPickup(ModelsUtils.getRandomLocation());
		scheduleDelivery.setDropoff(ModelsUtils.getRandomLocation());
		scheduleDelivery.setDeadline(deliveryRequest.getDeadline());
		scheduleDelivery.setExpedited(deliveryRequest.isExpedited());
		scheduleDelivery
				.setConfirmationRequired(ModelsConverter.getConfirmationType(deliveryRequest.getConfirmationRequired()));
		scheduleDelivery.setDroneId(droneId);

		return scheduleDelivery;
	}
}
