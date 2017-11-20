package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.tests;
//package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.tests;
//
//import static com.github.tomakehurst.wiremock.client.WireMock.aResponse;
//import static com.github.tomakehurst.wiremock.client.WireMock.get;
//import static com.github.tomakehurst.wiremock.client.WireMock.stubFor;
//import static com.github.tomakehurst.wiremock.client.WireMock.urlPathMatching;
//import static org.junit.Assert.assertEquals;
//import static org.junit.Assert.assertTrue;
//
//import java.io.IOException;
//import java.io.InputStream;
//import java.util.Arrays;
//import java.util.Date;
//import java.util.List;
//import java.util.Map;
//import java.util.Random;
//import java.util.Scanner;
//import java.util.UUID;
//import java.util.concurrent.ExecutionException;
//
//import org.apache.http.client.fluent.Content;
//import org.apache.http.client.fluent.Request;
//import org.apache.http.nio.reactor.IOReactorException;
//import org.junit.Before;
//import org.junit.Ignore;
//import org.junit.Rule;
//import org.junit.Test;
//import org.springframework.http.client.reactive.ClientHttpConnector;
//import org.springframework.web.context.request.async.DeferredResult;
//import org.springframework.web.reactive.function.client.WebClient;
//
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.SchedulerSettings;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DeliverySchedule;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageGen;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ConfirmationRequired;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ContainerSize;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.AccountServiceCallerImpl;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.DeliveryServiceCallerImpl;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.DroneSchedulerServiceCallerImpl;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.PackageServiceCallerImpl;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.ThirdPartyServiceCallerImpl;
//import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ConfigReader;
//import com.github.tomakehurst.wiremock.core.WireMockConfiguration;
//import com.github.tomakehurst.wiremock.junit.WireMockRule;
//
////import wiremock.org.apache.http.HttpResponse;
////import wiremock.org.apache.http.client.ClientProtocolException;
////import wiremock.org.apache.http.impl.client.CloseableHttpClient;
////import wiremock.org.apache.http.impl.client.HttpClients;
//
//public class TestBackendServices {
////	
////	@ClassRule
////	public static WireMockClassRule wireMockRule = new WireMockClassRule(8089);
//	
////	@Rule
////	public WireMockRule wireMockRule = new WireMockRule(WireMockConfiguration.options().port(8888).httpsPort(8889));
//
//	private final String DroneServiceReturnValuePrefix = "AssignedDroneId";
//
//	private Random random = new Random();
//
//	private final String Locations[] = { "Austin", "Seattle", "Berkley", "Oregon", "Florida", "Blaine", "Renton" };
//
//	private final ContainerSize[] containers = ContainerSize.values();
//	private final ConfirmationRequired[] confirmations = ConfirmationRequired.values();
//	
////	private final static String ServerUri = "http://localhost:8888";
////	private final WebClient httpClient = WebClient.create(ClientHttpConnector);
//
//	@Before
//	public void setUp() throws Exception {
//		Map<String, String> configSet = ConfigReader.readAllConfigurationValues("Config.properties");
//		
//		// Uris for backend services
//		SchedulerSettings.AccountServiceUri = configSet.get("account.service.uri");
//		SchedulerSettings.DeliveryServiceUri = configSet.get("delivery.service.uri");
//		SchedulerSettings.DroneSchedulerServiceUri = configSet.get("dronescheduler.service.uri");
//		SchedulerSettings.PackageServiceUri = configSet.get("package.service.uri");
//		SchedulerSettings.ThirdPartyServiceUri = configSet.get("thirdparty.service.uri");
//	}
////	
////	private void setupStubAccountService() {
////		// Account service stub
////		stubFor(get(urlPathMatching("/api/Account/.*")).willReturn(
////				aResponse().withStatus(200).withHeader("Content-Type", "text/plain").withBody("true")));
////	}
////	
////	private String convertHttpResponseToString(HttpResponse httpResponse) throws IOException {
////	    InputStream inputStream = httpResponse.getEntity().getContent();
////	    return convertInputStreamToString(inputStream);
////	}
////	
////	private String convertInputStreamToString(InputStream inputStream) {
////	    Scanner scanner = new Scanner(inputStream, "UTF-8");
////	    String string = scanner.useDelimiter("\\Z").next();
////	    scanner.close();
////	    return string;
////	}
////	
////	@Test
////	public void CanRetrieveAccountStatusFromMockService() throws ClientProtocolException, IOException{
////		setupStubAccountService();
//////		verify(getRequestedFor(urlEqualTo("/api/Account/someRandomId")));
////
////		Content content = Request.Get(ServerUri+"/api/Account/someRandomId").execute().returnContent();
////		
////		assertEquals("true", content.toString());
////		
////		//HttpResponse httpResponse = httpClient.execute(request);
////		//String stringResponse = convertHttpResponseToString(httpResponse);
//////		assertEquals(200, httpResponse.getStatusLine().getStatusCode());
//////		assertEquals("application/json", httpResponse.getFirstHeader("Content-Type").getValue());
////		//assertEquals("true", stringResponse);
////	}
//	
////	@Test
////	public void CanRetrieveAccountStatusFromAccountService()
////			throws InterruptedException, ExecutionException, IOReactorException {
////		String accountId = UUID.randomUUID().toString();
////
////		AccountServiceCallerImpl accountService = new AccountServiceCallerImpl();
////		assertEquals(true, accountService.isAccountActive(accountId, SchedulerSettings.AccountServiceUri));
////	}
//
//	@Test
//	public void CanRetrieveAccountStatusFromAccountServiceAsync() throws InterruptedException, ExecutionException {
//		String accountId = UUID.randomUUID().toString();
//		AccountServiceCallerImpl accountService = new AccountServiceCallerImpl();
//
//		DeferredResult<Boolean> result = accountService.isAccountActiveAsync(accountId, SchedulerSettings.AccountServiceUri);
//
//		result.onCompletion(() -> {
//			assertEquals(true, result.getResult());
//		});
//	}
//
////	@Test
////	public void CanRetrieveThirdPartyConsentFromService() throws InterruptedException, ExecutionException {
////		// TODO: Revisit this implementation since response body parameters are
////		// split though passing one works
////		ThirdPartyServiceCallerImpl thirdpartySvc = new ThirdPartyServiceCallerImpl();
////		assertEquals(false, thirdpartySvc.isThirdPartyServiceRequired("Idaho", SchedulerSettings.ThirdPartyServiceUri));
////	}
//
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
//
////	@Test
////	public void CanRetrieveDroneIdFromDroneDeliveryService() throws InterruptedException, ExecutionException {
////		DroneSchedulerServiceCallerImpl droneSvc = new DroneSchedulerServiceCallerImpl();
////		Delivery deliveryRequest = this.createDeliveryRequest();
////		String droneId = droneSvc.getDroneId(deliveryRequest, SchedulerSettings.DroneSchedulerServiceUri);
////		System.out.println(droneId);
////		assertTrue(DroneServiceReturnValuePrefix, droneId.startsWith(DroneServiceReturnValuePrefix));
////	}
//
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
//
////	@Test
////	public void CanScheduleDeliveryWithDeliveryService() throws InterruptedException, ExecutionException {
////		String droneId = UUID.randomUUID().toString();
////		DeliveryServiceCallerImpl deliverySvc = new DeliveryServiceCallerImpl();
////		DeliverySchedule delivery = deliverySvc.scheduleDelivery(this.createDeliveryRequest(), droneId,
////				SchedulerSettings.DeliveryServiceUri);
////
////		assertEquals(droneId, delivery.getDroneId());
////	}
//
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
//
////	@Test
////	public void CanRetrievePackagesInfoFromPackageService() throws InterruptedException, ExecutionException {
////		PackageInfo pack = new PackageInfo();
////
////		pack.setPackageId(UUID.randomUUID().toString());
////		pack.setSize(containers[random.nextInt(containers.length)]);
////
////		PackageServiceCallerImpl packageSvc = new PackageServiceCallerImpl();
////		List<PackageGen> packs = packageSvc.createPackages(Arrays.asList(pack), SchedulerSettings.PackageServiceUri);
////
////		assertEquals(1, packs.size());
////		assertEquals(pack.getPackageId(), packs.get(0).getTag());
////	}
//
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
//
//	private Delivery createDeliveryRequest() {
//		PackageInfo pack = new PackageInfo();
//
//		pack.setPackageId(UUID.randomUUID().toString());
//		pack.setSize(containers[random.nextInt(containers.length)]);
//
//		Delivery delivery = new Delivery();
//
//		delivery.setDeliveryId(UUID.randomUUID().toString());
//		delivery.setOwnerId(UUID.randomUUID().toString());
//		delivery.setPickupTime(new Date());
//		delivery.setDropOffLocation(Locations[random.nextInt(Locations.length)]);
//		delivery.setPickupLocation(Locations[random.nextInt(Locations.length)]);
//		delivery.setConfirmationRequired(confirmations[random.nextInt(confirmations.length)]);
//		delivery.setDeadline("LineOfDeadPeople");
//		delivery.setExpedited(true);
//
//		delivery.setPackageInfo(pack);
//
//		return delivery;
//	}
//}
