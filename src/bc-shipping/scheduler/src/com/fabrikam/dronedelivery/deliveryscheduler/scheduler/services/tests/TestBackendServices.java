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

import java.io.IOException;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import org.junit.Before;
import org.junit.ClassRule;
import org.junit.Test;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpMethod;
import org.springframework.http.ResponseEntity;
import org.springframework.web.client.AsyncRestTemplate;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageGen;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ModelsUtils;
import com.github.tomakehurst.wiremock.junit.WireMockClassRule;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import wiremock.org.eclipse.jetty.http.HttpStatus;

public class TestBackendServices {
	@ClassRule
	public static WireMockClassRule wiremock = new WireMockClassRule(wireMockConfig().dynamicPort().dynamicHttpsPort());
	
	public final String baseUri = "http://localhost:" + wiremock.port();

	private AsyncRestTemplate restTemplate = new AsyncRestTemplate();
	
    private static Gson deserializer = new GsonBuilder().create();

	@Before
	public void setUp() throws Exception {
		// Account service stub
		wiremock.stubFor(get(urlPathMatching("/api/Account/.*")).willReturn(
				aResponse().withStatus(HttpStatus.OK_200).withHeader("Content-Type", "text/plain").withBody("true")));
		
		// Third party deliveries service stub
		wiremock.stubFor(put(urlPathEqualTo("/api/ThirdPartyDeliveries/some-random-delivery-id"))
				.withRequestBody(equalToJson(deserializer.toJson(ModelsUtils.getRandomLocation(0.0))))
				.willReturn(
				aResponse().withStatus(HttpStatus.CREATED_201)
				.withBody("false")));
		
		// Package service stub
		PackageInfo packInfo = ModelsUtils.getPackageInfo("some-random-tag");
		String json = deserializer.toJson(packInfo);
		wiremock.stubFor(put(urlPathEqualTo("/api/packages/some-random-tag"))
				.withRequestBody(equalToJson(json))
				.willReturn(
				aResponse().withStatus(HttpStatus.CREATED_201)
				.withHeader("Content-Type", "application/json")
				.withBody(json)));

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
		String uri = this.baseUri + "/api/ThirdPartyDeliveries/some-random-delivery-id";
		String json = deserializer.toJson(ModelsUtils.getRandomLocation(0.0));
		HttpEntity<String> entity = new HttpEntity<String>(json);
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(
				this.restTemplate.exchange(uri, HttpMethod.PUT, entity, String.class));
		String result = ((HttpEntity<String>)cfuture.get()).getBody();
		assertEquals(result, "false");
	}
	
	@Test
	public void CanRetrievePackageFromMockServiceAsync() throws IOException, InterruptedException, ExecutionException {
		String uri = this.baseUri + "/api/packages/some-random-tag";
		PackageInfo packInfo = ModelsUtils.getPackageInfo("some-random-tag");
		HttpEntity<String> entity = new HttpEntity<String>(deserializer.toJson(packInfo));
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(
				this.restTemplate.exchange(uri, HttpMethod.PUT, entity, String.class));
		HttpEntity<String> resultEntity = cfuture.get();
		PackageGen result = (PackageGen)ModelsUtils.getPackageGen(resultEntity.getBody());
		assertEquals(result.getTag(), "some-random-tag");
	}
	
//	@Test
//	public void CanRetrieveAccountStatusFromAccountServiceAsync() throws InterruptedException, ExecutionException {
//			
//		Map<String, String> properties = new HashMap<String,String>();
//		SchedulerSettings.AccountServiceUri = "http://51.179.155.83/api/Account";
//		DeliveryRequestEventProcessor.invokeAccountServiceAsync(this.createDeliveryRequest(), properties);
		
//		CompletableFuture<?> cfuture = toCompletableFuture(
//				accountService.getData("http://51.179.155.83/api/Account/", accountId));
//		String result = ((HttpEntity<String>)cfuture.get()).getBody();
//		assertEquals(result, "true");
//	}


//	@Test
//	public void CanRetrieveThirdPartyConsentFromServiceAsync() throws InterruptedException, ExecutionException {
//		// TODO: Revisit this implementation since response body parameters are
//		// split though passing one works
//		ThirdPartyServiceCallerImpl thirdpartySvc = new ThirdPartyServiceCallerImpl();
//		DeferredResult<Boolean> result = thirdpartySvc.isThirdPartyServiceRequiredAsync("Idaho", SchedulerSettings.ThirdPartyServiceUri);
//		result.onCompletion(() -> {
//			assertEquals(false, result.getResult());
//		});
//	}

//	@Test
//	public void CanRetrieveDroneIdFromDroneDeliveryServiceAsync() throws InterruptedException, ExecutionException {
//		DroneSchedulerServiceCallerImpl droneSvc = new DroneSchedulerServiceCallerImpl();
//		Delivery deliveryRequest = this.createDeliveryRequest();
//		DeferredResult<String> droneId = droneSvc.getDroneIdAsync(deliveryRequest, SchedulerSettings.DroneSchedulerServiceUri);
//
//		droneId.onCompletion(() -> {
//			assertTrue(DroneServiceReturnValuePrefix,
//					((String) droneId.getResult()).startsWith(DroneServiceReturnValuePrefix));
//		});
//	}

//	@Test
//	public void CanScheduleDeliveryWithDeliveryServiceAsync() throws InterruptedException, ExecutionException {
//		String droneId = UUID.randomUUID().toString();
//		DeliveryServiceCallerImpl deliverySvc = new DeliveryServiceCallerImpl();
//		DeferredResult<DeliverySchedule> deliveryScheduled = deliverySvc
//				.scheduleDeliveryAsync(this.createDeliveryRequest(), droneId, SchedulerSettings.DeliveryServiceUri);
//
//		deliveryScheduled.onCompletion(() -> {
//			DeliverySchedule delivery = (DeliverySchedule) deliveryScheduled.getResult();
//			assertEquals(droneId, delivery.getDroneId());
//		});
//	}

//	@Test
//	public void CanRetrievePackagesInfoFromPackageServiceAsync() throws InterruptedException, ExecutionException {
//		PackageInfo pack = new PackageInfo();
//
//		pack.setPackageId(UUID.randomUUID().toString());
//		pack.setSize(containers[random.nextInt(containers.length)]);
//
//		PackageServiceCallerImpl packageSvc = new PackageServiceCallerImpl();
//		DeferredResult<PackageGen> defResult = packageSvc.createPackageAsync(pack,
//				SchedulerSettings.PackageServiceUri);
//
//		defResult.onCompletion(() -> {
//			PackageGen packGen = (PackageGen) defResult.getResult();
//			assertEquals(pack.getPackageId(), packGen.getTag());
//		});
//	}
}

