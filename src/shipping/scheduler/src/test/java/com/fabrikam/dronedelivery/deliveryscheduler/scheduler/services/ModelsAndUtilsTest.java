package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import static org.junit.Assert.assertEquals;

import java.io.IOException;

import org.junit.Before;
import org.junit.Rule;
import org.junit.Test;
import org.junit.contrib.java.lang.system.EnvironmentVariables;
import org.springframework.util.Assert;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ModelsUtils;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.google.gson.Gson;

public class ModelsAndUtilsTest {

	private Gson serializer;
	private ObjectMapper objMapper;

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
	public void can_serialize_delivery_with_gson() {
		Delivery delivery = ModelsUtils.createDeliveryRequest();
		String jsonDelivery = serializer.toJson(delivery);
		Delivery rehydratedDelivery = serializer.fromJson(jsonDelivery, Delivery.class);
		Assert.isInstanceOf(Delivery.class, rehydratedDelivery, "Delivery serialized correctly");
	}

	@Test
	public void can_serialize_delivery_with_jackson() throws IOException {
		Delivery delivery = ModelsUtils.createDeliveryRequest();
		String jsonDelivery = objMapper.writeValueAsString(delivery);
		Delivery rehydratedDelivery = objMapper.readValue(jsonDelivery, Delivery.class);
		Assert.isInstanceOf(Delivery.class, rehydratedDelivery, "Delivery serialized correctly");
	}

	@Test
	public void can_serialize_specific_workload_correctly() {
		String jsonPayload = "{\"OwnerId\":\"f0a8680e-574a-4d30-89a6-9c8d8ca2302d\",\"DeliveryId\":\"37bbe418-2254-4ebf-a848-bd9929f10b75\",\"Packages\":[{\"PackageId\":\"5114e11d-0e23-47d4-a36c-2a297c83037d\",\"Size\":1}],\"deadline\":\"LineOfDeadlyZombiatedPeople\",\"confirmationRequired\":3,\"expedited\":false,\"DropoffLocation\":\"Seattle\",\"PickupTime\":\"Jul 28, 2017 04:06:24 PM\",\"PickupLocation\":\"Florida\"}";
		Delivery delivery = serializer.fromJson(jsonPayload, Delivery.class);
		Assert.isInstanceOf(Delivery.class, delivery);
	}

	@Test
	public void can_set_env_vars_correctly() {
		environmentVariables.set(envHostNameString, "mypod-1");
		assertEquals("mypod-1", System.getenv(envHostNameString));
	}
	
	@Test
	public void can_parse_http_proxy_correctly(){
		environmentVariables.set(envHttpProxyString, "k8s-agent-f1cfb2cf-0:4140");
    	String[] address = System.getenv(envHttpProxyString).split("\\s*:\\s*");
    	assertEquals(address[0], "k8s-agent-f1cfb2cf-0");
    	assertEquals(Integer.parseInt(address[1]), 4140);
	}

	@Test
	public void can_parse_stateful_set_config_correctly() {
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
}
