package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.tests;

import static org.junit.Assert.assertEquals;

import java.util.Collections;
import java.util.Date;
import java.util.Random;
import java.util.UUID;
import java.util.concurrent.ExecutionException;

import org.junit.Before;
import org.junit.Test;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;

import org.springframework.util.concurrent.ListenableFuture;
import org.springframework.web.client.AsyncRestTemplate;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ConfirmationRequired;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ContainerSize;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;

public class PostDeliveryRequests {

	private AsyncRestTemplate asyncRest;
	private HttpHeaders requestHeaders;
	private static String postUri = "http://23.101.149.1:8080/api/deliveryrequests";
	
	private final String Locations[] = {"Austin", "Seattle", "Berkley", "Oregon", "Florida", "Blaine", "Renton"}; 
	private static Random random = new Random();
		
	private final ContainerSize[] containers = ContainerSize.values();
	private final ConfirmationRequired[] confirmations = ConfirmationRequired.values();
	
	private final double leftLimit = 10D;
	private final double rightLimit = 100D;

	@Before
	public void setUp() throws Exception {
		asyncRest = new AsyncRestTemplate();
		requestHeaders = new HttpHeaders();
		requestHeaders.setAccept(Collections.singletonList(MediaType.APPLICATION_JSON));
		requestHeaders.setContentType(MediaType.APPLICATION_JSON_UTF8);		
		//addServiceMeshHeaders();
	}

	@Test
	public void PostDeliveryRequestsToWeb() throws InterruptedException, ExecutionException {
		for(int i=0;i<5000;i++){
			Delivery delivery = createDeliveryRequest();
			System.out.println(delivery.toString());

			HttpEntity<Delivery> entity = new HttpEntity<Delivery>(delivery, requestHeaders);

			ListenableFuture<ResponseEntity<Delivery>> response = asyncRest.postForEntity(postUri, entity,
					Delivery.class, delivery);

			assertEquals(HttpStatus.ACCEPTED, response.get().getStatusCode());
		}
	}
	
	private Delivery createDeliveryRequest() {
		PackageInfo pack = new PackageInfo();

		pack.setPackageId(UUID.randomUUID().toString());
		pack.setSize(containers[random.nextInt(containers.length)]);
		pack.setTag(UUID.randomUUID().toString());
		pack.setWeight(leftLimit + new Random().nextDouble() * (rightLimit - leftLimit));
		
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
