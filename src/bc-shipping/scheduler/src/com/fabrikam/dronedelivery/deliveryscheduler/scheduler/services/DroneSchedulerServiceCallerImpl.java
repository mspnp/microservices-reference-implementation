package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import static net.javacrumbs.futureconverter.springjava.FutureConverter.toCompletableFuture;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import org.apache.commons.lang3.exception.ExceptionUtils;

import org.springframework.http.HttpEntity;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.util.concurrent.ListenableFuture;
import org.springframework.util.concurrent.ListenableFutureCallback;
//import org.springframework.util.concurrent.ListenableFutureCallback;
import org.springframework.web.context.request.async.DeferredResult;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DroneDelivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.LocationRandomizer;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ModelsConverter;

public class DroneSchedulerServiceCallerImpl extends ServiceCallerImpl {

	// Calls the super constructor and sets the HTTP context
	public DroneSchedulerServiceCallerImpl() {
		super();
	}

	@Override
	public <T> ListenableFuture<?> postData(String url, T entity) {
		HttpEntity<T> httpEntity = new HttpEntity<T>(entity, this.getRequestHeaders());
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
	public String getDroneId(Delivery deliveryRequest, String uri) throws InterruptedException, ExecutionException {
		DroneDelivery delivery = createDroneDelivery(deliveryRequest);
		ListenableFuture<?> response = this.postData(uri, delivery);

		ResponseEntity<String> entity = (ResponseEntity<String>) response.get();
		return entity.getStatusCode() == HttpStatus.OK ? entity.getBody().toString() : null;
	}

	@SuppressWarnings("unchecked")
	public DeferredResult<String> getDroneIdAsync(Delivery deliveryRequest, String uri) {
		// Create DeferredResult with timeout 5s
		final DeferredResult<String> result = new DeferredResult<String>();

		// Create drone delivery to post data
		DroneDelivery delivery = createDroneDelivery(deliveryRequest);

		// Let's call the backend
		ListenableFuture<ResponseEntity<String>> future = (ListenableFuture<ResponseEntity<String>>) this.putData(uri+"/"+delivery.getDeliveryId(),
				delivery);
		
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(future);

		cfuture.thenAcceptAsync(response -> {
			if (response.getStatusCode() == HttpStatus.OK) {
				result.setResult(response.getBody());
			} else {
				result.setResult(null);
				result.setErrorResult(response.getStatusCode());
			}
		}).exceptionally(e -> {
			result.setErrorResult(ExceptionUtils.getStackTrace(e));
			result.setResult(null);
			return null;
		});
		
//		future.addCallback(new ListenableFutureCallback<ResponseEntity<String>>() {
//			@Override
//			public void onSuccess(ResponseEntity<String> response) {
//				// Will be called in HttpClient thread
//				if (response.getStatusCode() == HttpStatus.OK) {
//					result.setResult(response.getBody());
//				}else {
//					result.setResult(null);
//					result.setErrorResult(response.getStatusCode());
//				}
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

	private DroneDelivery createDroneDelivery(Delivery deliveryRequest) {
		DroneDelivery delivery = new DroneDelivery();
		delivery.setDeliveryId(deliveryRequest.getDeliveryId());

		// TODO: Convert string location to Location instead of using below hack
		delivery.setDropoff(LocationRandomizer.getRandomLocation());
		delivery.setPickup(LocationRandomizer.getRandomLocation());

		delivery.setExpedited(delivery.getExpedited());
		delivery.setPackageDetail(ModelsConverter.getPackageDetail(deliveryRequest.getPackageInfo()));

		return delivery;
	}
}
