package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import java.io.IOException;
import java.nio.charset.StandardCharsets;

import org.apache.commons.io.IOUtils;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import org.springframework.http.client.ClientHttpResponse;
import org.springframework.web.client.ResponseErrorHandler;

public class ServiceCallerResponseErrorHandler implements ResponseErrorHandler {

	private static final Logger log = LogManager.getLogger(ServiceCallerResponseErrorHandler.class);
	
	@Override
	public void handleError(ClientHttpResponse clienthttpresponse) throws IOException {
		if(clienthttpresponse.getStatusCode().value()>=400){
			throw new BackendServiceCallFailedException(IOUtils.toString(clienthttpresponse.getBody(), StandardCharsets.UTF_8.name()));
		}
	}

	@Override
	public boolean hasError(ClientHttpResponse clienthttpresponse) throws IOException {
	    if (clienthttpresponse.getStatusCode().value() >=400) {
	        log.error("Status code: {}", clienthttpresponse.getStatusCode());
	        log.error("Response: {}", clienthttpresponse.getStatusText());
	        log.error(clienthttpresponse.getBody());
	        return true;
	    }
	    
	    return false;
	}
}
