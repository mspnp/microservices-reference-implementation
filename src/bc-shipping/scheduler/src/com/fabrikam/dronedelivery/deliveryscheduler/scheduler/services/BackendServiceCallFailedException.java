package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

public class BackendServiceCallFailedException extends RuntimeException {

	private static final long serialVersionUID = 123456798765045L;
	private static final Logger Log = LogManager.getLogger(BackendServiceCallFailedException.class);

	public BackendServiceCallFailedException(String message) {
		super(message);
		Log.debug("BackendServiceCallFailedException raised: {}", message);
		
	}

}
