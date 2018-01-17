package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import org.apache.commons.lang3.exception.ExceptionUtils;
//import org.apache.http.nio.reactor.IOReactorException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.util.concurrent.ListenableFuture;


import static net.javacrumbs.futureconverter.springjava.FutureConverter.*;

public final class AccountServiceCallerImpl extends ServiceCallerImpl {

	private Boolean isAccountActive = false;
	
	// Calls the super constructor and sets the HTTP context
	public AccountServiceCallerImpl() {
		super();
	}

	@Override
	public <T> ListenableFuture<?> getData(String url, T data) {
		String accountId = (String) data;
		url = url.concat(accountId);
		return getAsyncRestTemplate().getForEntity(url, String.class);
	}

	public boolean isAccountActive(String accountId, String uri) throws InterruptedException, ExecutionException {
		ListenableFuture<?> accountMock = this.getData(uri + '/', accountId);
		return Boolean.valueOf(((ResponseEntity<?>) accountMock.get()).getBody().toString());
	}

	
	@SuppressWarnings("unchecked")
	public Boolean isAccountActiveAsync(String accountId, String uri) {
		// Let's call the backend
		ListenableFuture<ResponseEntity<String>> future = (ListenableFuture<ResponseEntity<String>>) this
				.getData(uri + '/', accountId);
		
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(future);
		
		cfuture.thenAcceptAsync(response -> {
			if (response.getStatusCode() == HttpStatus.OK) {
				isAccountActive = Boolean.valueOf(response.getBody());
			} else {
				throw new BackendServiceCallFailedException(response.getStatusCode().getReasonPhrase());
			}
		}).exceptionally(e -> {
			throw new BackendServiceCallFailedException(ExceptionUtils.getStackTrace(e));
		});

		return isAccountActive;
	}
}
