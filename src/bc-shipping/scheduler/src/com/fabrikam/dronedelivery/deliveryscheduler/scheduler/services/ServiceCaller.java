package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import org.springframework.util.concurrent.ListenableFuture;

public interface ServiceCaller {
	<T> ListenableFuture<?> getData(String url, T data);
	
	<T> ListenableFuture<?> postData(String url, T entity);
	
	<T> ListenableFuture<?> putData(String url, T entity, Object... args);
}
