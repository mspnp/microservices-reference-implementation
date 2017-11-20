package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import org.apache.commons.lang3.exception.ExceptionUtils;
//import org.apache.http.nio.reactor.IOReactorException;
import org.springframework.http.ResponseEntity;
import org.springframework.util.concurrent.ListenableFuture;
import org.springframework.util.concurrent.ListenableFutureCallback;
//import org.springframework.util.concurrent.ListenableFutureCallback;
import org.springframework.web.context.request.async.DeferredResult;

import static net.javacrumbs.futureconverter.springjava.FutureConverter.*;

public final class AccountServiceCallerImpl extends ServiceCallerImpl {

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
	public DeferredResult<Boolean> isAccountActiveAsync(String accountId, String uri) {
		// Create DeferredResult with timeout 5s
		final DeferredResult<Boolean> result = new DeferredResult<Boolean>();

		// Let's call the backend
		ListenableFuture<ResponseEntity<String>> future = (ListenableFuture<ResponseEntity<String>>) this
				.getData(uri + '/', accountId);
		
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(future);
		
		cfuture.thenAcceptAsync(response ->	result.setResult(Boolean.valueOf(response.getBody())))
			   .exceptionally(e -> {
					result.setErrorResult(ExceptionUtils.getStackTrace(e));
					result.setResult(null);
					return null;
			   });
		
//		future.addCallback(new ListenableFutureCallback<ResponseEntity<String>>() {
//			@Override
//			public void onSuccess(ResponseEntity<String> response) {
//				// Will be called in HttpClient thread
//				result.setResult(Boolean.valueOf(response.getBody()));
//			}
//
//			@Override
//			public void onFailure(Throwable t) {
//				result.setErrorResult(ExceptionUtils.getStackTrace(t));
//				result.setResult(null);
//			}
//		});
		

		
		// Return the thread to servlet container, the response will be
		// processed by another thread.
		return result;
	}
}
