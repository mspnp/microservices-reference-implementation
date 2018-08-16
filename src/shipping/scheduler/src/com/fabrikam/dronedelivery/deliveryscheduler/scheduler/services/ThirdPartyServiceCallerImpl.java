package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import static net.javacrumbs.futureconverter.springjava.FutureConverter.toCompletableFuture;

import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import org.apache.commons.lang3.exception.ExceptionUtils;

import org.springframework.http.HttpEntity;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.util.concurrent.ListenableFuture;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.Location;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.LocationRandomizer;

public class ThirdPartyServiceCallerImpl extends ServiceCallerImpl {
	
	private Boolean isThirdPartyRequired = true;
	private String exceptionMsg = null;

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
	public Boolean isThirdPartyServiceRequiredAsync(String dropOffLocation, String uri) {

		Location pickup = LocationRandomizer.getRandomLocation();

		// Let's call the backend
		ListenableFuture<ResponseEntity<String>> future = (ListenableFuture<ResponseEntity<String>>) this
				.putData(uri + '/' + UUID.randomUUID().toString(), pickup);

		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(future);

		cfuture.thenAcceptAsync(response -> {
			if (response.getStatusCode() == HttpStatus.OK) {
				isThirdPartyRequired = Boolean.valueOf(response.getBody());
			} else {
				throw new BackendServiceCallFailedException(response.getStatusCode().getReasonPhrase());
			}
		}).exceptionally(e -> {
			exceptionMsg = ExceptionUtils.getStackTrace(e);
			return null;
		});
		
		if(isThirdPartyRequired==null){
			throw new BackendServiceCallFailedException(exceptionMsg);
		}

		return isThirdPartyRequired;
	}
}
