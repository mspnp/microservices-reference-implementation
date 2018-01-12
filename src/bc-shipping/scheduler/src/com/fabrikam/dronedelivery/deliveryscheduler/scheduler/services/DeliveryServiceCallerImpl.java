package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import static net.javacrumbs.futureconverter.springjava.FutureConverter.toCompletableFuture;

import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import org.apache.commons.lang3.exception.ExceptionUtils;
//import org.apache.http.nio.reactor.IOReactorException;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.util.concurrent.ListenableFuture;


import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.DeliverySchedule;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.UserAccount;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ModelsConverter;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.ModelsUtils;

public class DeliveryServiceCallerImpl extends ServiceCallerImpl {

	private DeliverySchedule  deliverySchedule = null;
	
	// Calls the super constructor and sets the HTTP context
	public DeliveryServiceCallerImpl() {
		super();
	}

	@Override
	public <T> ListenableFuture<?> postData(String url, T entity) {
		HttpEntity<T> httpEntity = new HttpEntity<T>(entity, this.getRequestHeaders());
		ListenableFuture<ResponseEntity<DeliverySchedule>> response = getAsyncRestTemplate().postForEntity(url,
				httpEntity, DeliverySchedule.class, entity);

		return response;
	}
	
	@Override
	public <T> ListenableFuture<?> putData(String url, T entity, Object... args) {
		HttpEntity<T> httpEntity = new HttpEntity<T>(entity, this.getRequestHeaders());
		ListenableFuture<ResponseEntity<DeliverySchedule>> response = getAsyncRestTemplate().exchange(url, HttpMethod.PUT, httpEntity, DeliverySchedule.class);
		return response;
	}

	@SuppressWarnings("unchecked")
	public DeliverySchedule scheduleDelivery(Delivery deliveryRequest, String droneId, String uri)
			throws InterruptedException, ExecutionException {
		DeliverySchedule schedule = this.createDeliverySchedule(deliveryRequest, droneId);
		ListenableFuture<?> response = this.postData(uri, schedule);

		ResponseEntity<DeliverySchedule> entity = (ResponseEntity<DeliverySchedule>) response.get();
		return entity.getStatusCode() == HttpStatus.CREATED ? entity.getBody() : null;
	}

	
	@SuppressWarnings("unchecked")
	public DeliverySchedule scheduleDeliveryAsync(Delivery deliveryRequest, String droneId,
			String uri) {

		// Create delivery schedule to post as data
		DeliverySchedule schedule = this.createDeliverySchedule(deliveryRequest, droneId);

		// Let's call the backend
		ListenableFuture<ResponseEntity<DeliverySchedule>> future = (ListenableFuture<ResponseEntity<DeliverySchedule>>) this
				.putData(uri + '/' + schedule.getId(), schedule);

		CompletableFuture<ResponseEntity<DeliverySchedule>> cfuture = toCompletableFuture(future);

		cfuture.thenAcceptAsync(response -> {
			deliverySchedule = response.getBody();
			if (response.getStatusCode() != HttpStatus.CREATED) {
				throw new BackendServiceCallFailedException(response.getStatusCode().getReasonPhrase());
			}
		}).exceptionally(e -> {
			throw new BackendServiceCallFailedException(ExceptionUtils.getStackTrace(e));
		});
		
		future = null;
		cfuture = null;
		deliveryRequest = null;
		schedule = null;

		return deliverySchedule;
	}
	
	private DeliverySchedule createDeliverySchedule(Delivery deliveryRequest, String droneId) {
		UserAccount account = new UserAccount(UUID.randomUUID().toString(), deliveryRequest.getOwnerId());

		DeliverySchedule scheduleDelivery = new DeliverySchedule();
		scheduleDelivery.setId(deliveryRequest.getDeliveryId());
		scheduleDelivery.setOwner(account);
		scheduleDelivery.setPickup(ModelsUtils.getRandomLocation());
		scheduleDelivery.setDropoff(ModelsUtils.getRandomLocation());
		//scheduleDelivery.setPackageId(deliveryRequest.getPackageInfo().getPackageId());
		scheduleDelivery.setDeadline(deliveryRequest.getDeadline());
		scheduleDelivery.setExpedited(deliveryRequest.isExpedited());
		scheduleDelivery
				.setConfirmationRequired(ModelsConverter.getConfirmationType(deliveryRequest.getConfirmationRequired()));
		scheduleDelivery.setDroneId(droneId);

		return scheduleDelivery;
	}
}
