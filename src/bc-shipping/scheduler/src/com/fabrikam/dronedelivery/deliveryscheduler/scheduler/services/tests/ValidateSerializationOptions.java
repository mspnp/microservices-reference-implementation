package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.tests;

import static org.junit.Assert.*;

import java.io.IOException;
import java.util.Date;
import java.util.Random;
import java.util.UUID;

import org.junit.Before;
import org.junit.Rule;
import org.junit.Test;
import org.springframework.util.Assert;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ConfirmationRequired;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ContainerSize;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.google.gson.Gson;
import org.junit.contrib.java.lang.system.EnvironmentVariables;

public class ValidateSerializationOptions {

	private Gson serializer;
	private ObjectMapper objMapper;
	private final String Locations[] = { "Austin", "Seattle", "Berkley", "Oregon", "Florida", "Blaine", "Renton" };
	private final static Random random = new Random();

	private final ContainerSize[] containers = ContainerSize.values();
	private final ConfirmationRequired[] confirmations = ConfirmationRequired.values();

	private static final String envHostNameString = "HOST_POD_NAME";
	private static final String envHttpProxyString = "http_proxy";

	@Rule
	public final EnvironmentVariables environmentVariables = new EnvironmentVariables();

	@Before
	public void setUp() throws Exception {
		serializer = new Gson();
		objMapper = new ObjectMapper();
	}

	@Test
	public void ValidateSerializationWithGson() {
		Delivery delivery = createDelivery();
		String jsonDelivery = serializer.toJson(delivery);
		Delivery rehydratedDelivery = serializer.fromJson(jsonDelivery, Delivery.class);
		Assert.isInstanceOf(Delivery.class, rehydratedDelivery, "Delivery serialized correctly");
	}

	@Test
	public void ValidateSerializationWithJackson() throws IOException {
		Delivery delivery = createDelivery();
		String jsonDelivery = objMapper.writeValueAsString(delivery);
		Delivery rehydratedDelivery = objMapper.readValue(jsonDelivery, Delivery.class);
		Assert.isInstanceOf(Delivery.class, rehydratedDelivery, "Delivery serialized correctly");
	}

	@Test
	public void ValidateParseDeliveryWithSpecificPayload() {
		String jsonPayload = "{\"OwnerId\":\"f0a8680e-574a-4d30-89a6-9c8d8ca2302d\",\"DeliveryId\":\"37bbe418-2254-4ebf-a848-bd9929f10b75\",\"Packages\":[{\"PackageId\":\"5114e11d-0e23-47d4-a36c-2a297c83037d\",\"Size\":1}],\"deadline\":\"LineOfDeadlyZombiatedPeople\",\"confirmationRequired\":3,\"expedited\":false,\"DropoffLocation\":\"Seattle\",\"PickupTime\":\"Jul 28, 2017 04:06:24 PM\",\"PickupLocation\":\"Florida\"}";
		Delivery delivery = serializer.fromJson(jsonPayload, Delivery.class);
		Assert.isInstanceOf(Delivery.class, delivery);
	}

	@Test
	public void validateSetEnvironmentVariable() {
		environmentVariables.set(envHostNameString, "mypod-1");
		assertEquals("mypod-1", System.getenv(envHostNameString));
	}
	
	@Test
	public void validateSetEnvironmentVariableHttpProxy(){
		environmentVariables.set(envHttpProxyString, "k8s-agent-f1cfb2cf-0:4140");
    	String[] address = System.getenv(envHttpProxyString).split("\\s*:\\s*");
    	assertEquals(address[0], "k8s-agent-f1cfb2cf-0");
    	assertEquals(Integer.parseInt(address[1]), 4140);
	}

	@Test
	public void validateParseEnvironmentVariable() {
		environmentVariables.set(envHostNameString, "mypod-1");
		String hostName = System.getenv(envHostNameString);
		int partitionId = -1;
		if (hostName != null) {
			String trimmedHostName = hostName.trim();
			if (trimmedHostName != "") {
				partitionId = Integer.parseInt(trimmedHostName.substring(trimmedHostName.indexOf('-') + 1));
			}
		}

		assertEquals(1, partitionId);
	}

	private Delivery createDelivery() {
		PackageInfo pack = new PackageInfo();
		pack.setSize(containers[random.nextInt(containers.length)]);
		pack.setPackageId(UUID.randomUUID().toString());

		Delivery delivery = new Delivery();

		delivery.setOwnerId(UUID.randomUUID().toString());
		delivery.setPickupTime(new Date());
		delivery.setDropOffLocation(Locations[random.nextInt(Locations.length)]);
		delivery.setPickupLocation(Locations[random.nextInt(Locations.length)]);
		delivery.setConfirmationRequired(confirmations[random.nextInt(confirmations.length)]);

		delivery.setPackageInfo(pack);

		return delivery;
	}
}
