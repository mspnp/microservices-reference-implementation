package com.fabrikam.dronedelivery.deliveryscheduler.scheduler;

import com.fabrikam.dronedelivery.deliveryscheduler.akkareader.AkkaDelivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.StorageQueue.StorageQueueClientFactory;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DeliverySchedule;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageGen;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services.*;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.JsonSyntaxException;
import com.microsoft.azure.iot.iothubreact.MessageFromDevice;
import com.microsoft.azure.storage.StorageException;
import com.microsoft.azure.storage.queue.CloudQueue;
import com.microsoft.azure.storage.queue.CloudQueueClient;
import com.microsoft.azure.storage.queue.CloudQueueMessage;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.logging.log4j.CloseableThreadContext;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.springframework.http.HttpHeaders;

import java.net.URISyntaxException;
import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.EnumMap;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

public class DeliveryRequestEventProcessor {
    private static final Logger Log = LogManager.getLogger(DeliveryRequestEventProcessor.class);

    private static final String CorrelationHeaderTag = "CorrelationId";

    private static Gson deserializer = new GsonBuilder().setPrettyPrinting().create();

    

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
            Delivery delivery = deserializer.fromJson(dataReceived, Delivery.class);
            deliveryRequest = new AkkaDelivery();
            deliveryRequest.setDelivery(delivery);
            deliveryRequest.setMessageFromDevice(message);
        } catch (JsonSyntaxException | IllegalStateException e) {
            Log.error("throwable: {}", ExceptionUtils.getStackTrace(e).toString());
        }

        return deliveryRequest;
    }

    public static CompletableFuture<DeliverySchedule> processDeliveryRequestAsync(Delivery deliveryRequest,
                                                               Map<String, String> properties) {
        DeliverySchedule deliverySchedule = null;
        // Extract the correlation id and log it
        String correlationId = properties.get(SchedulerSettings.CorrelationHeader);
        try (final CloseableThreadContext.Instance ctc = CloseableThreadContext.put(CorrelationHeaderTag,
                correlationId)) {
            Log.info("Processing delivery request: Calling backend services for delivery id: {}",
                    deliveryRequest.getDeliveryId());
            
               Boolean isAccountActive = invokeAccountServiceAsync(deliveryRequest, properties);

               if (isAccountActive) {
                  Log.info("Account is {}", (isAccountActive ? "active." : "suspended."));
                  Boolean isThirdPartyRequired = invokeThirdPartyServiceAsync(deliveryRequest, properties);
                  Log.info("Third party is {}", (isThirdPartyRequired ? "required." : "not required."));

                  if (!isThirdPartyRequired) {
                      PackageGen packageGen = invokePackageServiceAsync(deliveryRequest, properties);
                      if (packageGen != null) {
                          Log.info("Package generated: {}", packageGen.toString());
                          String droneId = invokeDroneSchedulerServiceAsync(deliveryRequest, properties);
  
                          if (droneId != null) {
                              Log.info("Drone assigned: {}", droneId);
                              deliverySchedule = invokeDeliverySchedulerServiceAsync(deliveryRequest, droneId, properties);
                          }
                      }
                  }
              }
        }
        
        return CompletableFuture.completedFuture(deliverySchedule);

    }

    public static Boolean invokeAccountServiceAsync(Delivery deliveryRequest, Map<String, String> properties) {
        Boolean accountResult = new Boolean(false);
        try {
            AccountServiceCallerImpl backendService = (AccountServiceCallerImpl) backendServicesMap.get(ServiceName.AccountService);
            appendServiceMeshHeaders(backendService, properties);
            accountResult = backendService.isAccountActiveAsync(deliveryRequest.getOwnerId(), SchedulerSettings.AccountServiceUri);
        } catch (Exception e) {
            // Assume failure of service here - a crude supervisor
            // implementation
            superviseFailureAsync(deliveryRequest, ServiceName.AccountService, ExceptionUtils.getMessage(e))
                    .thenRunAsync(() -> {
                        Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
                    });

            throw e;
        }

        return accountResult;
    }

    private static Boolean invokeThirdPartyServiceAsync(Delivery deliveryRequest, Map<String, String> properties) {
        Boolean thirdPartyResult = new Boolean(false);
        try {
            ThirdPartyServiceCallerImpl backendService = (ThirdPartyServiceCallerImpl) backendServicesMap
                    .get(ServiceName.ThirdPartyService);
            appendServiceMeshHeaders(backendService, properties);
            thirdPartyResult = backendService.isThirdPartyServiceRequiredAsync(deliveryRequest.getDropOffLocation(),
                    SchedulerSettings.ThirdPartyServiceUri);
        } catch (Exception e) {
            // Assume failure of service here - a crude supervisor
            // implementation
            superviseFailureAsync(deliveryRequest, ServiceName.ThirdPartyService, ExceptionUtils.getMessage(e))
                    .thenRunAsync(() -> {
                        Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
                    });

            throw e;
        }

        return thirdPartyResult;
    }

    private static String invokeDroneSchedulerServiceAsync(Delivery deliveryRequest, Map<String, String> properties) {
        String droneScheduleResult = null;
        try {
            DroneSchedulerServiceCallerImpl backendService = (DroneSchedulerServiceCallerImpl) backendServicesMap
                    .get(ServiceName.DroneSchedulerService);
            appendServiceMeshHeaders(backendService, properties);
            droneScheduleResult = backendService.getDroneIdAsync(deliveryRequest,
                    SchedulerSettings.DroneSchedulerServiceUri);
        } catch (Exception e) {
            // Assume failure of service here - a crude supervisor
            // implementation
            superviseFailureAsync(deliveryRequest, ServiceName.DroneSchedulerService, ExceptionUtils.getMessage(e))
                    .thenRunAsync(() -> {
                        Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
                    });

            throw e;
        }

        return droneScheduleResult;
    }

    public static PackageGen invokePackageServiceAsync(Delivery deliveryRequest, Map<String, String> properties) {
        PackageGen packageResult = new PackageGen();
        try {
            PackageInfo packageInfo = deliveryRequest.getPackageInfo();
            PackageServiceCallerImpl backendService = (PackageServiceCallerImpl) backendServicesMap.get(ServiceName.PackageService);
            appendServiceMeshHeaders(backendService, properties);
            packageResult = backendService.createPackageAsync(packageInfo, SchedulerSettings.PackageServiceUri);
        } catch (Exception e) {
            // Assume failure of service here - a crude supervisor
            // implementation
            superviseFailureAsync(deliveryRequest, ServiceName.PackageService, ExceptionUtils.getMessage(e))
                    .thenRunAsync(() -> {
                        Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
                    });
            throw e;
        }

        return packageResult;
    }

    private static DeliverySchedule invokeDeliverySchedulerServiceAsync(Delivery deliveryRequest,
                                                                        String droneId, Map<String, String> properties) {
        DeliverySchedule deliveryResult = null;
        try {
            DeliveryServiceCallerImpl backendService = (DeliveryServiceCallerImpl) backendServicesMap.get(ServiceName.DeliveryService);
            appendServiceMeshHeaders(backendService, properties);
            deliveryResult = backendService.scheduleDeliveryAsync(deliveryRequest, droneId, SchedulerSettings.DeliveryServiceUri);
        } catch (Exception e) {
            // Assume failure of service here - a crude supervisor
            // implementation
            superviseFailureAsync(deliveryRequest, ServiceName.DeliveryService, ExceptionUtils.getMessage(e))
                    .thenRunAsync(() -> {
                        Log.error("throwable: {}", ExceptionUtils.getStackTrace(e));
                    });

            throw e;
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

    /*
     * Supervisor implementation
     */
    private static CompletableFuture<Void> superviseFailureAsync(Delivery deliveryRequest, ServiceName serviceName, String errorMessage) {
        CloudQueueClient queueClient = StorageQueueClientFactory.get();
        try {
            CloudQueue queueReference = queueClient.getQueueReference(SchedulerSettings.storageQueueName);
            queueReference.createIfNotExists();

            String requestInJson = deserializer.toJson(deliveryRequest, Delivery.class);
            byte[] requestInJsonBytes = requestInJson.getBytes(StandardCharsets.UTF_8);
            CloudQueueMessage message = new CloudQueueMessage(requestInJsonBytes);

            queueReference.addMessage(message);

        } catch (URISyntaxException | StorageException e) {
            e.printStackTrace();
        } finally {
            return null;
        }

    }
}
