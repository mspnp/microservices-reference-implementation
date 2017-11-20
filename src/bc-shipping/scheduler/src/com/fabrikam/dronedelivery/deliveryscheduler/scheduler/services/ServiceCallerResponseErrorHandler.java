package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import java.io.IOException;

import org.springframework.http.client.ClientHttpResponse;
import org.springframework.web.client.DefaultResponseErrorHandler;
import org.springframework.web.client.ResponseErrorHandler;

public class ServiceCallerResponseErrorHandler implements ResponseErrorHandler {

	private ResponseErrorHandler errorHandler = new DefaultResponseErrorHandler();
	
	@Override
	public void handleError(ClientHttpResponse response) throws IOException {
		errorHandler.handleError(response);
	}

	@Override
	public boolean hasError(ClientHttpResponse response) throws IOException {
		return errorHandler.hasError(response);
	}
}
