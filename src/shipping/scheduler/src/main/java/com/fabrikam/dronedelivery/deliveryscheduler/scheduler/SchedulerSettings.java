package com.fabrikam.dronedelivery.deliveryscheduler.scheduler;

import java.util.ArrayList;
import java.util.List;

public class SchedulerSettings {
	// Service URIs
	public static String DeliveryServiceUri;
	public static String PackageServiceUri;
	public static String DroneSchedulerServiceUri;
	public static String AccountServiceUri;
	public static String ThirdPartyServiceUri;
	
	public static List<String> ServiceMeshHeaders = new ArrayList<String>();
	public static String CorrelationHeader;
	public static String HttpProxyValue;
	public static String HostNameValue;

	//StorageQueue
	public static String StorageQueueConnectionString;
	public static String StorageQueueName;
	public static int CheckpointTimeInMinutes;
}
