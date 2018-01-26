package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import java.util.Collections;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.util.concurrent.ListenableFuture;
import org.springframework.web.client.AsyncRestTemplate;

public abstract class ServiceCallerImpl implements ServiceCaller {
	private HttpHeaders requestHeaders;

	public AsyncRestTemplate getAsyncRestTemplate() {
		AsyncRestTemplate asyncRestTemplate = new AsyncRestTemplate(CustomClientHttpRequestFactory.INSTANCE.get());
		asyncRestTemplate.setErrorHandler(new ServiceCallerResponseErrorHandler());
		return asyncRestTemplate;
	}

	public HttpHeaders getRequestHeaders() {
		return requestHeaders;
	}

	public void setRequestHeaders(HttpHeaders requestHeaders) {
		this.requestHeaders = requestHeaders;
	}

	@Override
	public <T> ListenableFuture<?> getData(String url, T data) {
		return null;
	}

	public ServiceCallerImpl() {
		// Initialize http headers
		initHttpHeaders();
	}

	private void initHttpHeaders() {
		this.requestHeaders = new HttpHeaders();
		this.requestHeaders.setAccept(Collections.singletonList(MediaType.APPLICATION_JSON));
		this.requestHeaders.setContentType(MediaType.APPLICATION_JSON_UTF8);
	}

	@Override
	public <T> ListenableFuture<?> postData(String url, T entity) {
		return null;
	}
	
	@Override
	public <T> ListenableFuture<?> putData(String url, T entity, Object... args) {
		return null;
	}
}