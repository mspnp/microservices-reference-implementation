package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import static net.javacrumbs.futureconverter.springjava.FutureConverter.toCompletableFuture;

import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import org.apache.commons.lang3.exception.ExceptionUtils;

import org.springframework.http.HttpEntity;
import org.springframework.http.HttpMethod;
import org.springframework.http.ResponseEntity;
import org.springframework.util.concurrent.ListenableFuture;
import org.springframework.util.concurrent.ListenableFutureCallback;
import org.springframework.web.context.request.async.DeferredResult;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.Location;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.LocationRandomizer;

public class ThirdPartyServiceCallerImpl extends ServiceCallerImpl {

	public ThirdPartyServiceCallerImpl() {
		super();
	}

	@Override
	public <T> ListenableFuture<?> postData(String url, T entity) {
		HttpEntity<T> httpEntity = new HttpEntity<T>(entity);
		ListenableFuture<ResponseEntity<String>> response = getAsyncRestTemplate().postForEntity(url, httpEntity,
				String.class, entity);

		return response;
	}

	@Override
	public <T> ListenableFuture<?> putData(String url, T entity, Object... args) {
		HttpEntity<T> httpEntity = new HttpEntity<T>(entity, this.getRequestHeaders());
		ListenableFuture<ResponseEntity<String>> response = getAsyncRestTemplate().exchange(url, HttpMethod.PUT, httpEntity, String.class);

		return response;
	}
	
	@SuppressWarnings("unchecked")
	public boolean isThirdPartyServiceRequired(String pickupLocation, String uri)
			throws InterruptedException, ExecutionException {
		// TODO: Convert string location to Location instead of using the below
		// hack
		Location pickup = LocationRandomizer.getRandomLocation();
		ListenableFuture<?> response = this.postData(uri, pickup);
		return Boolean.valueOf(((ResponseEntity<String>) response.get()).getBody().toString());
	}

	@SuppressWarnings("unchecked")
	public DeferredResult<Boolean> isThirdPartyServiceRequiredAsync(String dropOffLocation, String uri) {
		// Create DeferredResult with timeout 5s
		final DeferredResult<Boolean> result = new DeferredResult<Boolean>();

		Location pickup = LocationRandomizer.getRandomLocation();

		// Let's call the backend
		ListenableFuture<ResponseEntity<String>> future = (ListenableFuture<ResponseEntity<String>>) this
				.putData(uri + '/' + UUID.randomUUID().toString(), pickup);

		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(future);

		cfuture.thenAcceptAsync(response -> result.setResult(Boolean.valueOf(response.getBody()))).exceptionally(e -> {
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
