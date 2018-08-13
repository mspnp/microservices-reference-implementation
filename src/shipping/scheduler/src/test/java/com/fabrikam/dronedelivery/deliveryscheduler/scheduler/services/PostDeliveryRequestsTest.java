package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import static org.junit.Assert.assertEquals;

import java.util.Collections;
import java.util.concurrent.ExecutionException;

import org.junit.Before;
import org.junit.Ignore;
import org.junit.Test;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.util.concurrent.ListenableFuture;
import org.springframework.web.client.AsyncRestTemplate;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ModelsUtils;

public class PostDeliveryRequestsTest {

	private AsyncRestTemplate asyncRest;
	private HttpHeaders requestHeaders;
	
	// Set below to match with your deployment
	private static String postUri = "http://ingestion:8080/api/deliveryrequests";

	@Before
	public void setUp() throws Exception {
		asyncRest = new AsyncRestTemplate();
		requestHeaders = new HttpHeaders();
		requestHeaders.setAccept(Collections.singletonList(MediaType.APPLICATION_JSON));
		requestHeaders.setContentType(MediaType.APPLICATION_JSON_UTF8);		
	}

	/*
	 * Ignoring for Maven build since Uri needs to be set to run this test
	 */
	@Ignore
	@Test
	public void can_post_delivery_request() throws InterruptedException, ExecutionException {
		Delivery delivery = ModelsUtils.createDeliveryRequest();
		System.out.println(delivery.toString());

		HttpEntity<Delivery> entity = new HttpEntity<Delivery>(delivery, requestHeaders);

		ListenableFuture<ResponseEntity<Delivery>> response = asyncRest.postForEntity(postUri, entity,
				Delivery.class, delivery);

		assertEquals(HttpStatus.ACCEPTED, response.get().getStatusCode());
	}
}
