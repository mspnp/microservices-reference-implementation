package com.fabrikam.dronedelivery.deliveryscheduler.akkareader;

import java.time.temporal.ChronoUnit;
import java.util.Arrays;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

import java.util.stream.Collectors;

import org.apache.commons.lang3.StringUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

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

	public static void main(String args[]) {
		Map<String, String> configSet = ConfigReader.readAllConfigurationValues("Config.properties");

		// Uris for backend services and other env variables
		SchedulerSettings.AccountServiceUri = System.getenv(configSet.get("env.account.service.uri.key"));
		SchedulerSettings.DeliveryServiceUri = System.getenv(configSet.get("env.delivery.service.uri.key"));
		SchedulerSettings.DroneSchedulerServiceUri = System.getenv(configSet.get("env.dronescheduler.service.uri.key"));
		SchedulerSettings.PackageServiceUri = System.getenv(configSet.get("env.package.service.uri.key"));
		SchedulerSettings.ThirdPartyServiceUri = System.getenv(configSet.get("env.thirdparty.service.uri.key"));
		SchedulerSettings.ServiceMeshHeaders = Arrays
				.asList(System.getenv(configSet.get("env.service.mesh.headers.key")).split("\\s*,\\s*"));
		SchedulerSettings.CorrelationHeader = System.getenv(configSet.get("env.service.mesh.correlation.header.key"));
		SchedulerSettings.HostNameValue = System.getenv(configSet.get("env.hostname.key"));
		SchedulerSettings.HttpProxyValue = System.getenv(configSet.get("env.proxyname.key"));

		SchedulerSettings.StorageQueueConnectionString = System
				.getenv(configSet.get("env.storage.queue.connection.string"));
		SchedulerSettings.StorageQueueName = System.getenv(configSet.get("env.storage.queue.name"));
		SchedulerSettings.CheckpointTimeInMinutes = Integer
				.parseInt(System.getenv(configSet.get("env.checkpoint.time")));

		List<Integer> partitionsList = getPartitionsList();
		String partitionNumber = partitionsList.stream().map(Object::toString).collect(Collectors.joining(","));

		Log.info("Reading from partitions: {}", partitionNumber);

		SourceOptions options;
		// either we read from time
		// or from last known checkpoint
		if (SchedulerSettings.CheckpointTimeInMinutes > 0) {
			options = new SourceOptions().partitions(partitionsList).fromTime(
					java.time.Instant.now().minus(SchedulerSettings.CheckpointTimeInMinutes, ChronoUnit.MINUTES));
		} else {
			options = new SourceOptions().partitions(partitionsList).fromCheckpoint(null);
		}

		// .fromCheckpoint(java.time.Instant.now().minus(checkpointMin ,
		// ChronoUnit.MINUTES));
		// java.time.Instant.now().minus(4, ChronoUnit.HOURS)

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
	 * Implementation of Akka workflow in fluent mode
	 */

	private static Flow<AkkaDelivery, MessageFromDevice, NotUsed> deliveryProcessor() {
		return Flow.of(AkkaDelivery.class).map(delivery -> {
			CompletableFuture<DeliverySchedule> completableSchedule = DeliveryRequestEventProcessor
					.processDeliveryRequestAsync(delivery.getDelivery(), delivery.getMessageFromDevice().properties());

			completableSchedule.whenComplete((deliverySchedule, error) -> {
				if (error != null) {
					Log.error("Failed Delivery: {}", ExceptionUtils.getStackTrace(error).toString());
				} else {
					Log.info("Completed Delivery", deliverySchedule.toString());
				}

			});

			return delivery.getMessageFromDevice();
		});
	}
}
