package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.tests;

import static com.github.tomakehurst.wiremock.client.WireMock.aResponse;
import static com.github.tomakehurst.wiremock.client.WireMock.equalToJson;
import static com.github.tomakehurst.wiremock.client.WireMock.get;
import static com.github.tomakehurst.wiremock.client.WireMock.put;
import static com.github.tomakehurst.wiremock.client.WireMock.urlPathEqualTo;
import static com.github.tomakehurst.wiremock.client.WireMock.urlPathMatching;
import static com.github.tomakehurst.wiremock.core.WireMockConfiguration.wireMockConfig;
import static net.javacrumbs.futureconverter.springjava.FutureConverter.toCompletableFuture;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;

import java.io.IOException;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import org.junit.Before;
import org.junit.ClassRule;
import org.junit.Test;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpMethod;
import org.springframework.http.ResponseEntity;
import org.springframework.web.client.AsyncRestTemplate;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DeliverySchedule;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DroneDelivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageGen;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ModelsUtils;
import com.github.tomakehurst.wiremock.junit.WireMockClassRule;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.opentable.extension.BodyTransformer;

import wiremock.org.eclipse.jetty.http.HttpStatus;

public class TestBackendServices {
	@ClassRule
	public static WireMockClassRule wiremock = new WireMockClassRule(wireMockConfig().dynamicPort().dynamicHttpsPort().extensions(new BodyTransformer()));
	
	public final String baseUri = "http://localhost:" + wiremock.port();

	private final AsyncRestTemplate restTemplate = new AsyncRestTemplate();
	
    private final Gson deserializer = new GsonBuilder().create();
    
	private static final String DeliveryId = "some-random-delivery-id";
	
    private static final String Tag = "some-random-tag";
    
    private static final String DroneId = UUID.randomUUID().toString();
    
	private final PackageInfo packInfo = ModelsUtils.getPackageInfo(Tag);
	
	private final Delivery delivery = ModelsUtils.createDeliveryRequest();
	
	private final DroneDelivery droneDelivery = ModelsUtils.createDroneDelivery(delivery);
	
	private final DeliverySchedule deliverySchedule = ModelsUtils.createDeliverySchedule(delivery, DroneId);

	@Before
	public void setUp() throws Exception {
		// Account service stub
		wiremock.stubFor(get(urlPathMatching("/api/Account/.*")).willReturn(
				aResponse().withStatus(HttpStatus.OK_200).withHeader("Content-Type", "text/plain").withBody("true")));
		
		// Third party deliveries service stub
		wiremock.stubFor(put(urlPathEqualTo("/api/ThirdPartyDeliveries/" + DeliveryId))
				.withRequestBody(equalToJson(deserializer.toJson(ModelsUtils.getRandomLocation(0.0))))
				.willReturn(
				aResponse().withStatus(HttpStatus.CREATED_201)
				.withBody("false")));
		
		// Package service stub
		String json = deserializer.toJson(packInfo);
		wiremock.stubFor(put(urlPathEqualTo("/api/packages/" + Tag))
				.withRequestBody(equalToJson(json))
				.willReturn(
				aResponse().withStatus(HttpStatus.CREATED_201)
				.withHeader("Content-Type", "application/json")
				.withBody(json)));
		
		// Drone service stub
		wiremock.stubFor(put(urlPathEqualTo("/api/DroneDeliveries/" + droneDelivery.getDeliveryId()))
				.withRequestBody(equalToJson(deserializer.toJson(droneDelivery)))
				.willReturn(aResponse().withStatus(HttpStatus.CREATED_201).withHeader("Content-Type", "text/plain")
						.withBody("AssignedDroneId$(uuid)").withTransformers("body-transformer")));
		
		// Delivery service stub
		String json0 = deserializer.toJson(deliverySchedule);
		wiremock.stubFor(put(urlPathEqualTo("/api/Deliveries/" + delivery.getDeliveryId()))
				.withRequestBody(equalToJson(json0))
				.willReturn(aResponse().withStatus(HttpStatus.CREATED_201).withHeader("Content-Type", "application/json")
						.withBody(json0)));

	}
	
	@Test
	public void CanRetrieveAccountStatusFromMockServiceAsync() throws IOException, InterruptedException, ExecutionException {
		String uri = this.baseUri + "/api/Account/some-random-account-id";

		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(
				this.restTemplate.getForEntity(uri, String.class));
		String result = ((HttpEntity<String>)cfuture.get()).getBody();
		assertEquals(result, "true");
	}
	
	@Test
	public void CanRetrieveThirdPartyStatusFromMockServiceAsync() throws IOException, InterruptedException, ExecutionException {
		String uri = this.baseUri + "/api/ThirdPartyDeliveries/" + DeliveryId;
		String json = deserializer.toJson(ModelsUtils.getRandomLocation(0.0));
		HttpEntity<String> entity = new HttpEntity<String>(json);
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(
				this.restTemplate.exchange(uri, HttpMethod.PUT, entity, String.class));
		String result = ((HttpEntity<String>)cfuture.get()).getBody();
		assertEquals(result, "false");
	}
	
	@Test
	public void CanRetrievePackageFromMockServiceAsync() throws IOException, InterruptedException, ExecutionException {
		String uri = this.baseUri + "/api/packages/" + Tag;
		HttpEntity<String> entity = new HttpEntity<String>(deserializer.toJson(packInfo));
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(
				this.restTemplate.exchange(uri, HttpMethod.PUT, entity, String.class));
		HttpEntity<String> resultEntity = cfuture.get();
		PackageGen result = (PackageGen)ModelsUtils.getPackageGen(resultEntity.getBody());
		assertEquals(result.getTag(), Tag);
	}
	
	@Test
	public void CanAssignDroneIdFromMockServiceAsync() throws InterruptedException, ExecutionException{
		String uri = this.baseUri + "api/DroneDeliveries/" + droneDelivery.getDeliveryId();
		HttpEntity<String> entity = new HttpEntity<String>(deserializer.toJson(droneDelivery));
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(
				this.restTemplate.exchange(uri, HttpMethod.PUT, entity, String.class));
		String result = ((HttpEntity<String>)cfuture.get()).getBody();
		assertTrue(result.startsWith("AssignedDroneId"));
	}
	
	@Test
	public void CanScheduleDeliveryFromMockServiceAsync() throws IOException, InterruptedException, ExecutionException {
		String uri = this.baseUri + "/api/Deliveries/" + deliverySchedule.getId();
		HttpEntity<String> entity = new HttpEntity<String>(deserializer.toJson(deliverySchedule));
		CompletableFuture<ResponseEntity<DeliverySchedule>> cfuture = toCompletableFuture(
				this.restTemplate.exchange(uri, HttpMethod.PUT, entity, DeliverySchedule.class));
		HttpEntity<DeliverySchedule> resultEntity = cfuture.get();
		DeliverySchedule result = (DeliverySchedule)resultEntity.getBody();
		assertEquals(result.getDroneId(), DroneId);
	}
}

