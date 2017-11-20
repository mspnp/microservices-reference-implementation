package com.fabrikam.dronedelivery.ingestion.configuration;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.springframework.aop.interceptor.AsyncUncaughtExceptionHandler;

import java.lang.reflect.Method;

public class AsyncExceptionHandler implements AsyncUncaughtExceptionHandler {

	private final static Logger log = LogManager.getLogger(AsyncExceptionHandler.class);

	@Override
	public void handleUncaughtException(final Throwable throwable, final Method method, final Object... obj) {

		log.error("Exception message - {}", throwable.getMessage());
		log.error("Method name - {}", method.getName());

		for (final Object param : obj) {
			log.error("Param - {}", param);
		}
	}
}
