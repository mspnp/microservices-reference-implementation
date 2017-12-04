package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.tests;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.DeliveryRequestEventProcessor;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.SchedulerSettings;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.Location;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ConfirmationRequired;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ContainerSize;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.LocationRandomizer;
import com.github.tomakehurst.wiremock.junit.WireMockClassRule;
import org.junit.Before;
import org.junit.ClassRule;
import org.junit.Test;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpMethod;
import org.springframework.http.ResponseEntity;
import org.springframework.web.client.AsyncRestTemplate;

import java.io.IOException;
import java.util.*;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import static com.github.tomakehurst.wiremock.client.WireMock.*;
import static com.github.tomakehurst.wiremock.core.WireMockConfiguration.wireMockConfig;
import static net.javacrumbs.futureconverter.springjava.FutureConverter.toCompletableFuture;
import static org.junit.Assert.assertEquals;

public class TestBackendServices {
	@ClassRule
	public static WireMockClassRule wiremock = new WireMockClassRule(wireMockConfig().dynamicPort().dynamicHttpsPort());
	
	public final String baseUri = "http://localhost:" + wiremock.port();

	private Random random = new Random();

	private final String Locations[] = { "Austin", "Seattle", "Berkley", "Oregon", "Florida", "Blaine", "Renton" };

	private final ContainerSize[] containers = ContainerSize.values();
	private final ConfirmationRequired[] confirmations = ConfirmationRequired.values();
	
	private AsyncRestTemplate restTemplate = new AsyncRestTemplate();

	@Before
	public void setUp() throws Exception {

	}
	
	
	private void setupStubAccountService() {
		// Account service stub
		wiremock.stubFor(get(urlPathMatching("/api/Account/.*")).willReturn(
				aResponse().withStatus(200).withHeader("Content-Type", "text/plain").withBody("true")));
	}
	
	private void setupStubThirdPartyService(){
		wiremock.stubFor(put(urlPathMatching("/api/ThirdPartyDeliveries/.*"))
				.withHeader("Content-Type", equalTo("application/json"))
				.withRequestBody(matchingJsonPath("{ \"altitude\": 0, \"latitude\": 0, \"longitude\": 0 }"))
				.willReturn(
				aResponse().withStatus(200)
				.withHeader("Content-Type", "application/json; charset=utf-8")
				.withBody("false")));
			
	}
	
	@Test
	public void CanRetrieveAccountStatusFromMockServiceAsync() throws IOException, InterruptedException, ExecutionException {
		setupStubAccountService();
		String uri = this.baseUri + "/api/Account/some-random-account-id";

		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(
				this.restTemplate.getForEntity(uri, String.class));
		String result = ((HttpEntity<String>)cfuture.get()).getBody();
		assertEquals(result, "true");
	}
	
	@Test
	public void CanRetrieveThirdPartyStatusFromMockServiceAsync() throws IOException, InterruptedException, ExecutionException {
		setupStubThirdPartyService();
		String uri = this.baseUri + "/api/ThirdPartyDeliveries/some-random-delivery-id";
		
		HttpEntity<Location> entity = new HttpEntity<Location>(LocationRandomizer.getRandomLocation());
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(
				this.restTemplate.exchange(uri, HttpMethod.PUT, entity, String.class));
		String result = ((HttpEntity<String>)cfuture.get()).getBody();
		assertEquals(result, "false");
	}
	
	@Test
	public void CanRetrieveAccountStatusFromAccountServiceAsync() throws InterruptedException, ExecutionException {
			
		Map<String, String> properties = new HashMap<String,String>();
		SchedulerSettings.AccountServiceUri = "http://51.179.155.83/api/Account";
		DeliveryRequestEventProcessor.invokeAccountServiceAsync(this.createDeliveryRequest(), properties);
		
//		CompletableFuture<?> cfuture = toCompletableFuture(
//				accountService.getData("http://51.179.155.83/api/Account/", accountId));
//		String result = ((HttpEntity<String>)cfuture.get()).getBody();
//		assertEquals(result, "true");
	}


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

	private Delivery createDeliveryRequest() {
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

