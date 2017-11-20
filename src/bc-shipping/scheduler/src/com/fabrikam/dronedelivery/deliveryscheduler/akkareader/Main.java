package com.fabrikam.dronedelivery.deliveryscheduler.akkareader;

import java.time.temporal.ChronoUnit;
import java.util.Arrays;
import java.util.List;
import java.util.Map;
import java.util.concurrent.atomic.AtomicLong;

import java.util.stream.Collectors;

import org.apache.commons.lang3.StringUtils;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.springframework.web.context.request.async.DeferredResult;
import org.springframework.web.context.request.async.DeferredResult.DeferredResultHandler;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.DeliveryRequestEventProcessor;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.SchedulerSettings;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DeliverySchedule;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ConfigReader;
import com.microsoft.azure.iot.iothubreact.MessageFromDevice;
import com.microsoft.azure.iot.iothubreact.SourceOptions;
import com.microsoft.azure.iot.iothubreact.javadsl.IoTHub;
import com.typesafe.config.ConfigFactory;

import akka.NotUsed;
import akka.stream.javadsl.Flow;
import akka.stream.javadsl.Source;

public class Main extends ReactiveStreamingApp {

	private static final Logger Log = LogManager.getLogger(Main.class);
	private static AtomicLong deliveryCounter = new AtomicLong(0);
	private static AtomicLong nullDeliveryCounter = new AtomicLong(0);

	public static void main(String args[]) {
		Map<String, String> configSet = ConfigReader.readAllConfigurationValues("Config.properties");

		// Uris for backend services and other env variables
		SchedulerSettings.AccountServiceUri = System.getenv(configSet.get("env.account.service.uri.key"));
		SchedulerSettings.DeliveryServiceUri = System.getenv(configSet.get("env.delivery.service.uri.key"));
		SchedulerSettings.DroneSchedulerServiceUri = System.getenv(configSet.get("env.dronescheduler.service.uri.key"));
		SchedulerSettings.PackageServiceUri = System.getenv(configSet.get("env.package.service.uri.key"));
		SchedulerSettings.ThirdPartyServiceUri = System.getenv(configSet.get("env.thirdparty.service.uri.key"));
		SchedulerSettings.ServiceMeshHeaders = Arrays.asList(System.getenv(configSet.get("env.service.mesh.headers.key")).split("\\s*,\\s*"));
		SchedulerSettings.CorrelationHeader= System.getenv(configSet.get("env.service.mesh.correlation.header.key"));
		SchedulerSettings.HostNameValue = System.getenv(configSet.get("env.hostname.key"));
		SchedulerSettings.HttpProxyValue = System.getenv(configSet.get("env.proxyname.key"));

		List<Integer> partitionsList = getPartitionsList();
		String partitions = partitionsList.stream().map(Object::toString).collect(Collectors.joining(","));
		Log.info("Reading from partitions: {}", partitions);

		// Read from the saved offsets if any else from the specified time
		SourceOptions options = new SourceOptions().partitions(partitionsList)
				.fromCheckpoint(java.time.Instant.now().minus(4, ChronoUnit.HOURS));

		IoTHub iotHub = new IoTHub();
		Source<MessageFromDevice, NotUsed> messages = iotHub.source(options);

		messages.map(msg -> DeliveryRequestEventProcessor.parseDeliveryRequest(msg))
				.filter(ad -> ad.getDelivery() != null).via(deliveryProcessor()).to(iotHub.checkpointSink())
				.run(streamMaterializer);
	}

	/*
	 * Retrieves the list of partitions either from env or config
	 */
	private static List<Integer> getPartitionsList() {
		// Read setting for the partition from environment variable first
		Integer partitionId = -1;
		if (StringUtils.isNotEmpty(SchedulerSettings.HostNameValue)) {
			partitionId = Integer.valueOf(SchedulerSettings.HostNameValue.trim().split("\\s*-\\s*")[1]);
		}

		List<Integer> partitionsList = null;
		if (partitionId >= 0) {
			partitionsList = (List<Integer>) Arrays.asList(partitionId);
		} else {
			// Read using ConfigFactory so as to have custom configuration for
			// each node in terms of partition(s) being read
			partitionsList = ConfigFactory.load().getIntList("iothub-react.connection.hubPartitions");
		}

		return partitionsList;
	}

	/*
	 * Implementation of workflow using fluent api
	 */
	private static Flow<AkkaDelivery, MessageFromDevice, NotUsed> deliveryProcessor() {
		return Flow.of(AkkaDelivery.class).map(delivery -> {
			DeferredResult<DeliverySchedule> schedule = DeliveryRequestEventProcessor
					.processDeliveryRequestAsync(delivery.getDelivery(), delivery.getMessageFromDevice().properties());
			schedule.setResultHandler(new DeferredResultHandler() {

				@Override
				public void handleResult(Object result) {
					final DeliverySchedule deliverySchedule = (DeliverySchedule) result;
					if (deliverySchedule == null) {
						Log.info("Failed Deliveries: {}", nullDeliveryCounter.incrementAndGet());
					} else {
						Log.info("Successful Deliveries: {}", deliveryCounter.incrementAndGet());
					}
				}
			});

			return delivery.getMessageFromDevice();
		});
	}
}
