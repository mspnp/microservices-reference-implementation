package com.fabrikam.dronedelivery.deliveryscheduler.scheduler;

import java.util.Arrays;
import java.util.EnumMap;
import java.util.Map;

import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.logging.log4j.CloseableThreadContext;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import org.springframework.http.HttpHeaders;
import org.springframework.web.context.request.async.DeferredResult;
import org.springframework.web.context.request.async.DeferredResult.DeferredResultHandler;

import com.fabrikam.dronedelivery.deliveryscheduler.akkareader.AkkaDelivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DeliverySchedule;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageGen;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.AccountServiceCallerImpl;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.DeliveryServiceCallerImpl;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.DroneSchedulerServiceCallerImpl;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.PackageServiceCallerImpl;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.ServiceCallerImpl;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.ThirdPartyServiceCallerImpl;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.JsonSyntaxException;
import com.microsoft.azure.iot.iothubreact.MessageFromDevice;

public class DeliveryRequestEventProcessor {
	private static final Logger Log = LogManager.getLogger(DeliveryRequestEventProcessor.class);
	
	private static final String CorrelationHeaderTag = "CorrelationId";

	private static Gson deserializer = new GsonBuilder().setPrettyPrinting().create();

	private static String droneId = null;
	private static boolean isAccountActive = false;
	private static boolean isThirdPartyRequired = true;
	private static PackageGen packageGen = null; 

	private static final EnumMap<ServiceName, ServiceCallerImpl> backendServicesMap = new EnumMap<ServiceName, ServiceCallerImpl>(
			ServiceName.class);

	// Ensures that only one instance of each backend service is created with
	// it's own connection pool
	static {
		backendServicesMap.put(ServiceName.AccountService, new AccountServiceCallerImpl());
		backendServicesMap.put(ServiceName.DeliveryService, new DeliveryServiceCallerImpl());
		backendServicesMap.put(ServiceName.DroneSchedulerService, new DroneSchedulerServiceCallerImpl());
		backendServicesMap.put(ServiceName.PackageService, new PackageServiceCallerImpl());
		backendServicesMap.put(ServiceName.ThirdPartyService, new ThirdPartyServiceCallerImpl());
	}
	
	public static AkkaDelivery parseDeliveryRequest(MessageFromDevice message) {
		AkkaDelivery deliveryRequest = null;

		try {

			String dataReceived = message.contentAsString();

			// TODO: Hacker's delight! String is the whole body, all we need is
			// json
			// dataReceived = dataReceived.substring(dataReceived.indexOf('{'),
			// dataReceived.lastIndexOf('}') + 1);

			// Log.info("Eventhub string is: " + dataReceived);
			Delivery delivery = deserializer.fromJson(dataReceived, Delivery.class);
			deliveryRequest = new AkkaDelivery();
			deliveryRequest.setDelivery(delivery);
			deliveryRequest.setMessageFromDevice(message);
		} catch (JsonSyntaxException e) {
			Log.error("throwable: {}", ExceptionUtils.getStackTrace(e).toString());
		}

		return deliveryRequest;
	}
	
	/*
	 * Workflow that calls all the backend services asynchronously and parallelize first four services
	 */
	public static DeferredResult<DeliverySchedule> processDeliveryRequestAsyncParallel(Delivery deliveryRequest, Map<String, String> properties) {
		final DeferredResult<DeliverySchedule> deliveryScheduleCaller = new DeferredResult<DeliverySchedule>(
				(long) 5000);

		// Extract the correlation id and log it
		String correlationId = properties.get(SchedulerSettings.CorrelationHeader);
		try (final CloseableThreadContext.Instance ctc = CloseableThreadContext.put(CorrelationHeaderTag,
				correlationId)) {

			Log.info("Processing delivery request: Calling backend services for delivery id: {}", deliveryRequest.getDeliveryId());

			invokeAccountServiceAsync(deliveryRequest, properties).setResultHandler(new DeferredResultHandler() {
				@Override
				public void handleResult(Object result) {
					isAccountActive = (boolean) result;
					Log.info("Account is {}", (isAccountActive ? "active." : "suspended."));
				}
			});
			
			invokeThirdPartyServiceAsync(deliveryRequest, properties).setResultHandler(new DeferredResultHandler() {
				@Override
				public void handleResult(Object result) {
					isThirdPartyRequired = (boolean) result;
					Log.info("Third party is {}", (isThirdPartyRequired ? "required." : "not required."));
				}
			});

			invokePackageServiceAsync(deliveryRequest, properties).setResultHandler(new DeferredResultHandler() {
				@Override
				public void handleResult(Object result) {
					packageGen = (PackageGen) result;
					if (packageGen != null) {
						Log.info("Package generated: {}", packageGen.toString());
					}
				}
			});

			invokeDroneSchedulerServiceAsync(deliveryRequest, properties).setResultHandler(new DeferredResultHandler() {
				@Override
				public void handleResult(Object result) {
					droneId = (String) result;
					Log.info("Drone assigned: {}", droneId);
				}
			});

			if (isAccountActive && !isThirdPartyRequired && packageGen != null && droneId != null) {
				invokeDeliverySchedulerServiceAsync(deliveryRequest, droneId, properties)
						.setResultHandler(new DeferredResultHandler() {
							@Override
							public void handleResult(Object result) {
								deliveryScheduleCaller.setResult((DeliverySchedule) result);
							}
						});
			}

		}
		
		return deliveryScheduleCaller;
	}
	
	public static DeferredResult<DeliverySchedule> processDeliveryRequestAsync(Delivery deliveryRequest,
			Map<String, String> properties) {
		final DeferredResult<DeliverySchedule> deliveryScheduleCaller = new DeferredResult<DeliverySchedule>(
				(long) 5000);
		// Extract the correlation id and log it
		String correlationId = properties.get(SchedulerSettings.CorrelationHeader);
		try (final CloseableThreadContext.Instance ctc = CloseableThreadContext.put(CorrelationHeaderTag,
				correlationId)) {
			Log.info("Processing delivery request: Calling backend services for delivery id: {}",
					deliveryRequest.getDeliveryId());

			invokeAccountServiceAsync(deliveryRequest, properties).setResultHandler(new DeferredResultHandler() {
				@Override
				public void handleResult(Object result) {
					boolean isAccountActive = (boolean) result;
					Log.info("Account is {}", (isAccountActive ? "active." : "suspended."));
					if (isAccountActive) {
						invokeThirdPartyServiceAsync(deliveryRequest, properties)
								.setResultHandler(new DeferredResultHandler() {
									@Override
									public void handleResult(Object result) {
										boolean isThirdPartyRequired = (boolean) result;
										Log.info("Third party is {}",
												(isThirdPartyRequired ? "required." : "not required."));
										if (!isThirdPartyRequired) {
											invokePackageServiceAsync(deliveryRequest, properties)
													.setResultHandler(new DeferredResultHandler() {

														@Override
														public void handleResult(Object result) {
															PackageGen packageGen = (PackageGen) result;
															if (packageGen != null) {
																Log.info("Package generated: {}", packageGen.toString());
																invokeDroneSchedulerServiceAsync(deliveryRequest,
																		properties).setResultHandler(
																				new DeferredResultHandler() {
																					@Override
																					public void handleResult(
																							Object result) {
																						droneId = (String) result;
																						if (droneId != null) {
																							Log.info(
																									"Drone assigned: {}",
																									droneId);
																							invokeDeliverySchedulerServiceAsync(
																									deliveryRequest,
																									droneId, properties)
																											.setResultHandler(
																													new DeferredResultHandler() {
																														@Override
																														public void handleResult(
																																Object result) {
																															deliveryScheduleCaller
																																	.setResult(
																																			(DeliverySchedule) result);
																														}
																													});
																						}
																					}
																				});
															}
														}
													});
										}
									}
								});
					}
				}
			});
		}

		return deliveryScheduleCaller;
	}


	private static DeferredResult<Boolean> invokeAccountServiceAsync(Delivery deliveryRequest, Map<String,String> properties) {
		DeferredResult<Boolean> accountResult = new DeferredResult<Boolean>((long) 5000);
		try {
			AccountServiceCallerImpl backendService = (AccountServiceCallerImpl) backendServicesMap.get(ServiceName.AccountService);
			appendServiceMeshHeaders(backendService, properties);
			accountResult = backendService.isAccountActiveAsync(deliveryRequest.getOwnerId(), SchedulerSettings.AccountServiceUri);
		} catch (Exception e) {
			Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
		}

		return accountResult;
	}

	private static DeferredResult<Boolean> invokeThirdPartyServiceAsync(Delivery deliveryRequest,
			Map<String, String> properties) {
		DeferredResult<Boolean> thirdPartyResult = new DeferredResult<Boolean>((long) 5000);
		try {
			ThirdPartyServiceCallerImpl backendService = (ThirdPartyServiceCallerImpl) backendServicesMap
					.get(ServiceName.ThirdPartyService);
			appendServiceMeshHeaders(backendService, properties);
			thirdPartyResult = backendService.isThirdPartyServiceRequiredAsync(deliveryRequest.getDropOffLocation(),
					SchedulerSettings.ThirdPartyServiceUri);
		} catch (Exception e) {
			Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
		}

		return thirdPartyResult;
	}

	private static DeferredResult<String> invokeDroneSchedulerServiceAsync(Delivery deliveryRequest, Map<String,String> properties) {
		DeferredResult<String> droneScheduleResult = new DeferredResult<String>((long) 5000);
		try {
			DroneSchedulerServiceCallerImpl backendService = (DroneSchedulerServiceCallerImpl) backendServicesMap
					.get(ServiceName.DroneSchedulerService);
			appendServiceMeshHeaders(backendService, properties);
			droneScheduleResult = backendService.getDroneIdAsync(deliveryRequest,
							SchedulerSettings.DroneSchedulerServiceUri);
		} catch (Exception e) {
			Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
		}

		return droneScheduleResult;
	}

	public static DeferredResult<PackageGen> invokePackageServiceAsync(Delivery deliveryRequest, Map<String,String> properties) {
		DeferredResult<PackageGen> packageResult = new DeferredResult<PackageGen>((long) 5000);
		try {
			PackageInfo packageInfo = deliveryRequest.getPackageInfo();
			PackageServiceCallerImpl backendService = (PackageServiceCallerImpl) backendServicesMap.get(ServiceName.PackageService);
			appendServiceMeshHeaders(backendService, properties);
			packageResult = backendService.createPackageAsync(packageInfo, SchedulerSettings.PackageServiceUri);
		} catch (Exception e) {
			Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
		}

		return packageResult;
	}

	private static DeferredResult<DeliverySchedule> invokeDeliverySchedulerServiceAsync(Delivery deliveryRequest,
			String droneId, Map<String,String> properties) {
		DeferredResult<DeliverySchedule> deliveryResult = new DeferredResult<DeliverySchedule>((long) 5000);
		try {
			DeliveryServiceCallerImpl backendService = (DeliveryServiceCallerImpl) backendServicesMap.get(ServiceName.DeliveryService);
			appendServiceMeshHeaders(backendService, properties);
			deliveryResult = backendService.scheduleDeliveryAsync(deliveryRequest, droneId, SchedulerSettings.DeliveryServiceUri);
		} catch (Exception e) {
			Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
		}

		return deliveryResult;
	}
	
	private static void appendServiceMeshHeaders(ServiceCallerImpl service, Map<String, String> properties) {
		HttpHeaders httpHeaders = service.getRequestHeaders();
		for (String headerName : SchedulerSettings.ServiceMeshHeaders) {
			String headerValue = properties.get(headerName);
			if (headerValue != null) {
				if (httpHeaders.containsKey(headerName)) {
					httpHeaders.replace(headerName, Arrays.asList(headerValue));
				} else {
					httpHeaders.add(headerName, headerValue);
				}
			}
		}
	}
}
