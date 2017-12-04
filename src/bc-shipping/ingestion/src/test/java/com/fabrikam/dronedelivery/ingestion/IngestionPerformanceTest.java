package com.fabrikam.dronedelivery.ingestion;

import static org.junit.Assert.*;
import com.google.gson.*;

import org.junit.Before;
import org.junit.BeforeClass;
import org.junit.Ignore;
import org.junit.Test;
//import org.junit.runner.RunWith;
import org.springframework.web.client.*;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Date;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.ExecutionException;

import org.springframework.util.concurrent.ListenableFuture;
//import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.HttpEntity;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.converter.StringHttpMessageConverter;
import org.springframework.http.converter.json.MappingJackson2HttpMessageConverter;

import com.fabrikam.dronedelivery.ingestion.models.*;
import com.fasterxml.jackson.databind.ObjectMapper;

public class IngestionPerformanceTest {

	private AsyncRestTemplate asyncRest;
	private HttpHeaders requestHeaders;
	private String uri;
	private String IpAddress;
	private String Port;

	@Before
	public void setUp() throws Exception {

		asyncRest = new AsyncRestTemplate();
		asyncRest.getMessageConverters().add(new MappingJackson2HttpMessageConverter());
		asyncRest.getMessageConverters().add(new StringHttpMessageConverter());
		requestHeaders = new HttpHeaders();
		requestHeaders.setAccept(Arrays.asList(MediaType.APPLICATION_JSON));
		// 157.55.175.96
		// 23.96.201.211
		// 52.179.159.74
		// 52.179.158.181
		IpAddress = "localhost";
		Port = "8080";

	}

	@Test
	@Ignore
	public void PostDeliveryAsync() throws InterruptedException, ExecutionException {

		uri = new StringBuffer().append("http://").append(IpAddress).append(":").append(Port)
				.append("/api/deliveryrequests").toString();
		PackageInfo pack = new PackageInfo();

		pack.setPackageId(UUID.randomUUID().toString());
		pack.setSize(ContainerSize.Medium);

		List<PackageInfo> Packages = new ArrayList<PackageInfo>();
		Packages.add(pack);

		ExternalDelivery delivery = new ExternalDelivery();
		Date date = new Date();

		delivery.setOwnerId(UUID.randomUUID().toString());
		delivery.setPickupTime(date);
		delivery.setDropOffLocation("Austin");
		delivery.setPickupLocation("Austin");
		delivery.setDeadline("deadline");
		delivery.setConfirmationRequired(ConfirmationRequired.Picture);

		delivery.setPackageInfo(pack);

		HttpEntity<ExternalDelivery> entity = new HttpEntity<ExternalDelivery>(delivery, requestHeaders);

		ListenableFuture<ResponseEntity<ExternalDelivery>> response = asyncRest.postForEntity(uri, entity,
				ExternalDelivery.class, delivery);

		System.out.println(response.get().getBody().getPickupTime());

		assertEquals(HttpStatus.ACCEPTED, response.get().getStatusCode());

	}

	@Test
	@Ignore
	public void DeleteCancelDeliveryAsync() throws InterruptedException, ExecutionException {

		UUID deliveryId = UUID.randomUUID();
		uri = new StringBuffer().append("http://").append(IpAddress).append(":").append(Port)
				.append("/api/canceldelivery/").append(deliveryId).toString();

		@SuppressWarnings("unchecked")
		ListenableFuture<ResponseEntity<String>> response = (ListenableFuture<ResponseEntity<String>>) asyncRest.delete(uri,
				deliveryId);

		response.get();
		// response.get().getBody();

		// assertEquals(HttpStatus.OK,response.get().getStatusCode());

	}

	@Test
	@Ignore
	public void GetProbeAsync() throws InterruptedException, ExecutionException {

		uri = new StringBuffer().append("http://").append(IpAddress).append(":").append(Port).append("/api/probe")
				.toString();

		ListenableFuture<ResponseEntity<String>> response = asyncRest.getForEntity(uri, String.class);
		assertEquals(HttpStatus.OK, response.get().getStatusCode());

	}

	@Test
	@Ignore
	public void CanConvertToJson() throws InterruptedException {

		PackageInfo pack = new PackageInfo();

		pack.setPackageId(UUID.randomUUID().toString());
		pack.setSize(ContainerSize.Medium);

		List<PackageInfo> Packages = new ArrayList<PackageInfo>();
		Packages.add(pack);

		ExternalDelivery delivery = new ExternalDelivery();
		Date date = new Date();

		delivery.setOwnerId(UUID.randomUUID().toString());
		delivery.setPickupTime(date);
		delivery.setDropOffLocation("Austin");
		delivery.setPickupLocation("Austin");
		delivery.setDeadline("deadline");
		delivery.setConfirmationRequired(ConfirmationRequired.Picture);

		delivery.setPackageInfo(pack);

		Gson gson = new GsonBuilder().create();
		System.out.println(gson.toJson(delivery));
		System.out.println(asJsonString(delivery));

	}

	@Test
	@Ignore
	public void CanDeserializeDelivery() throws InterruptedException {

		PackageInfo pacKage = new PackageInfo();
		pacKage.setSize(ContainerSize.Small);
		pacKage.setPackageId(UUID.randomUUID().toString());
		
		List<PackageInfo> Packages = new ArrayList<PackageInfo>();
		Packages.add(pacKage);

		ExternalDelivery delivery = new ExternalDelivery();
		Date date = new Date();
		delivery.setOwnerId(UUID.randomUUID().toString());
		delivery.setPickupTime(date);
		delivery.setDropOffLocation("Austin");
		delivery.setPickupLocation("Austin");
		delivery.setConfirmationRequired(ConfirmationRequired.Picture);
		delivery.setDeadline("dealine");
		delivery.setExpedited(true);
		delivery.setPackageInfo(pacKage);

		Gson gson = new GsonBuilder().create();

		DeliveryBase intdelivery = new Delivery(UUID.randomUUID().toString(), delivery.getOwnerId().toString(), delivery.getPickupLocation(),
				delivery.getDropOffLocation(), delivery.getPickupTime(), delivery.isExpedited(),
				delivery.getConfirmationRequired(), delivery.getPackageInfo(), delivery.getDeadline());

		String jsondelivery = gson.toJson(intdelivery);
		System.out.println(gson.toJson(jsondelivery));

	}

	@Test
	@Ignore
	public void CanConvertToJson2() throws InterruptedException {

		// ExternalRescheduledDelivery delivery =
		// new ExternalRescheduledDelivery();
		// delivery.setDeadline("deadline");
		// delivery.setDelivery(UUID.randomUUID());
		// delivery.setDropOffLocation("location");
		// delivery.setPickupLocation("pickuplocation");
		//
		// Gson gson = new GsonBuilder().create();
		// System.out.println(gson.toJson(delivery));
		// System.out.println(asJsonString(delivery));
		//

	}

	@BeforeClass
	public static void setUpBeforeClass() throws Exception {
	}

	private static String asJsonString(ExternalDelivery obj) {
		try {
			ObjectMapper mapper = new ObjectMapper();
			String jsonContent = mapper.writeValueAsString(obj);
			return jsonContent;
		} catch (Exception e) {
			throw new RuntimeException(e);
		}
	}

}
