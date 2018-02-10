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

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DroneDelivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.LocationRandomizer;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ModelsConverter;

public class DroneSchedulerServiceCallerImpl extends ServiceCallerImpl {

	private String droneId = null;
	private String exceptionMsg = null;
	
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
	
	@SuppressWarnings("unchecked")
	public String getDroneIdAsync(Delivery deliveryRequest, String uri) {
		// Create drone delivery to post data
		DroneDelivery delivery = createDroneDelivery(deliveryRequest);

		// Let's call the backend
		ListenableFuture<ResponseEntity<String>> future = (ListenableFuture<ResponseEntity<String>>) this.putData(uri+"/"+delivery.getDeliveryId(),
				delivery);
		
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(future);

		cfuture.thenAcceptAsync(response -> {
			droneId = response.getBody();
			if (response.getStatusCode() != HttpStatus.OK) {
				throw new BackendServiceCallFailedException(response.getStatusCode().getReasonPhrase());
			}
		}).exceptionally(e -> {
			exceptionMsg = ExceptionUtils.getStackTrace(e);
			return null;
		});
		
		if(droneId==null){
			throw new BackendServiceCallFailedException(exceptionMsg);
		}
		
		return droneId;
	}
}
